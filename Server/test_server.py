import json
import tempfile
import threading
import unittest
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

import server


class ApiTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.http = server.create_server('127.0.0.1', 0, Path(self.tmp.name))
        self.thread = threading.Thread(target=self.http.serve_forever, daemon=True)
        self.thread.start()
        self.base = 'http://127.0.0.1:%s' % self.http.server_port

    def tearDown(self):
        self.http.shutdown()
        self.http.server_close()
        self.thread.join()
        self.tmp.cleanup()

    def request(self, path, data=None, raw=None):
        payload = raw if raw is not None else (json.dumps(data).encode() if data is not None else None)
        req = Request(self.base + path, data=payload, headers={'Content-Type': 'application/json'})
        try:
            response = urlopen(req, timeout=3)
        except HTTPError as error:
            response = error
        with response:
            return response.status, json.load(response)

    def test_query_and_commands(self):
        status, body = self.request('/api/devices')
        self.assertEqual(status, 200)
        self.assertEqual(len(body['devices']), 4)
        status, body = self.request('/api/devices/fan-01/commands', {'command': 'start'})
        self.assertEqual(status, 200)
        self.assertTrue(body['device']['running'])
        self.request('/api/devices/fan-01/commands', {'command': 'alarm'})
        _, body = self.request('/api/devices')
        self.assertTrue(next(x for x in body['devices'] if x['id'] == 'fan-01')['alarm'])

    def test_invalid_requests(self):
        self.assertEqual(self.request('/api/devices/missing/commands', {'command': 'start'})[0], 404)
        self.assertEqual(self.request('/api/devices/fan-01/commands', {'command': 'fly'})[0], 400)
        self.assertEqual(self.request('/api/inspections', raw=b'{broken')[0], 400)
        self.assertEqual(self.request('/api/inspections', {'records': []})[0], 400)
        self.assertEqual(self.request('/api/inspections', {'records': [{'deviceId': 'missing', 'result': 'normal'}]})[0], 400)

    def test_submit_persists_and_deduplicates_retry(self):
        data = {'batchId': 'test-batch', 'records': [{'deviceId': 'pump-01', 'result': 'abnormal', 'note': '噪声', 'checkedAt': '2026-11-19T01:00:00Z'}]}
        self.assertEqual(self.request('/api/inspections', data)[0], 200)
        self.assertEqual(self.request('/api/inspections', data)[0], 200)
        lines = (Path(self.tmp.name) / 'inspections.jsonl').read_text(encoding='utf-8').splitlines()
        self.assertEqual(len(lines), 1)
        self.assertEqual(json.loads(lines[0])['records'][0]['note'], '噪声')


if __name__ == '__main__':
    unittest.main()
