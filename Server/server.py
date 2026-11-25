"""Unity 培训用本地 HTTP 服务。仅依赖 Python 3 标准库。"""
import argparse
import json
import threading
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse


def create_server(host='127.0.0.1', port=8080, data_dir=None):
    folder = Path(data_dir or Path(__file__).parent / 'data')
    folder.mkdir(parents=True, exist_ok=True)
    devices = {key: dict(id=key, name=name, running=False, doorOpen=False, alarm=False)
               for key, name in [('cabinet-01', '服务器机柜'), ('fan-01', '通风风机'),
                                 ('pump-01', '循环水泵'), ('control-01', '控制柜')]}
    lock = threading.Lock()
    journal = folder / 'inspections.jsonl'
    batch_ids = set()
    if journal.exists():
        for line in journal.read_text(encoding='utf-8').splitlines():
            if line.strip():
                batch_ids.add(json.loads(line)['batchId'])

    class Handler(BaseHTTPRequestHandler):
        def reply(self, status, body):
            payload = json.dumps(body, ensure_ascii=False).encode('utf-8')
            self.send_response(status)
            self.send_header('Content-Type', 'application/json; charset=utf-8')
            self.send_header('Content-Length', str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)

        def do_GET(self):
            if urlparse(self.path).path != '/api/devices':
                return self.reply(404, {'error': '接口不存在'})
            with lock:
                self.reply(200, {'devices': list(devices.values())})

        def do_POST(self):
            try:
                length = int(self.headers.get('Content-Length', '0'))
                if length <= 0 or length > 65536:
                    raise ValueError('请求体长度应在 1～65536 字节之间')
                data = json.loads(self.rfile.read(length))
                if not isinstance(data, dict):
                    raise ValueError('请求体必须为 JSON 对象')
            except (ValueError, UnicodeError) as error:
                return self.reply(400, {'error': str(error)})
            path = urlparse(self.path).path.strip('/').split('/')
            with lock:
                if len(path) == 4 and path[:2] == ['api', 'devices'] and path[3] == 'commands':
                    device = devices.get(path[2])
                    if device is None:
                        return self.reply(404, {'error': '设备不存在'})
                    command = data.get('command')
                    actions = {'start': ('running', True), 'stop': ('running', False),
                               'open': ('doorOpen', True), 'close': ('doorOpen', False),
                               'alarm': ('alarm', True), 'clearAlarm': ('alarm', False)}
                    if not isinstance(command, str) or command not in actions:
                        return self.reply(400, {'error': '未知设备指令'})
                    field, value = actions[command]
                    device[field] = value
                    return self.reply(200, {'device': device})
                if path != ['api', 'inspections']:
                    return self.reply(404, {'error': '接口不存在'})
                records, batch = data.get('records'), data.get('batchId')
                if not isinstance(batch, str) or not batch.strip() or not isinstance(records, list) or not records:
                    return self.reply(400, {'error': '需要 batchId 和非空 records'})
                for record in records:
                    if (not isinstance(record, dict) or not isinstance(record.get('deviceId'), str)
                            or record['deviceId'] not in devices or record.get('result') not in ('normal', 'abnormal')
                            or not isinstance(record.get('note', ''), str)):
                        return self.reply(400, {'error': '检查记录的设备、结果或备注不合法'})
                if batch not in batch_ids:
                    entry = dict(data, receivedAt=datetime.now(timezone.utc).isoformat())
                    try:
                        with journal.open('a', encoding='utf-8') as stream:
                            stream.write(json.dumps(entry, ensure_ascii=False) + '\n')
                    except OSError:
                        return self.reply(500, {'error': '保存记录失败，请重试'})
                    batch_ids.add(batch)
                self.reply(200, {'ok': True, 'batchId': batch, 'count': len(records)})

    return ThreadingHTTPServer((host, port), Handler)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=8080)
    args = parser.parse_args()
    http = create_server(port=args.port)
    print('Unity 培训接口：http://127.0.0.1:%s（Ctrl+C 停止）' % args.port, flush=True)
    try:
        http.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        http.server_close()
