"""启动隔离的本地接口与故障夹具，验证真实 Windows Player。"""
import importlib.util
import json
import subprocess
import tempfile
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('training_server', ROOT / 'Server/server.py')
server = importlib.util.module_from_spec(spec)
spec.loader.exec_module(server)


class FaultFixture(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith('/slow/'):
            time.sleep(2)
        body = b'{invalid' if self.path.startswith('/invalid/') else b'{"devices":[]}'
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        try:
            self.wfile.write(body)
        except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
            pass


if __name__ == '__main__':
    result_dir = ROOT / 'TestResults'
    result_dir.mkdir(exist_ok=True)
    result_file = result_dir / 'player-smoke.json'
    if result_file.exists():
        result_file.unlink()
    with tempfile.TemporaryDirectory() as tmp:
        api = server.create_server('127.0.0.1', 0, Path(tmp))
        fixture = ThreadingHTTPServer(('127.0.0.1', 0), FaultFixture)
        for instance in (api, fixture):
            threading.Thread(target=instance.serve_forever, daemon=True).start()
        try:
            process = subprocess.run([
                str(ROOT / 'Builds/Windows/UnityTrain.exe'), '-screen-width', '1600', '-screen-height', '900', '-screen-fullscreen', '0',
                '-logFile', str(result_dir / 'player.log'), '--training-smoke', '--training-output', str(result_dir),
                '--training-api', 'http://127.0.0.1:%s' % api.server_port,
                '--training-fixture', 'http://127.0.0.1:%s' % fixture.server_port,
            ], cwd=ROOT, timeout=100)
            result = json.loads(result_file.read_text(encoding='utf-8')) if result_file.exists() else {'passed': False, 'error': 'Player 未生成结果'}
            entries = (Path(tmp) / 'inspections.jsonl').read_text(encoding='utf-8').splitlines() if (Path(tmp) / 'inspections.jsonl').exists() else []
            result['server_batches'] = len(entries)
            if result.get('passed') and (len(entries) != 2 or len(json.loads(entries[0])['records']) != 4):
                result.update(passed=False, error='服务端检查记录与预期不符')
            result_file.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
            print(json.dumps(result, ensure_ascii=False, indent=2))
            raise SystemExit(0 if result['passed'] and process.returncode == 0 else 1)
        finally:
            api.shutdown()
            fixture.shutdown()
            api.server_close()
            fixture.server_close()
