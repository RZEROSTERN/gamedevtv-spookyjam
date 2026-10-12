#!/usr/bin/env python3
"""Serve Unity's compressed Web build locally with the same encoding as itch.io."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class WebHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith('.gz'):
            path = path[:-3]
        if path.endswith('.wasm'):
            return 'application/wasm'
        if path.endswith('.data'):
            return 'application/octet-stream'
        return super().guess_type(path)

    def end_headers(self):
        path = self.translate_path(self.path)
        if Path(path).is_file() and path.endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('Builds/Web'))
    parser.add_argument('--port', type=int, default=8082)
    args = parser.parse_args()
    if not (args.directory / 'index.html').is_file():
        parser.error('The directory must contain a built index.html.')
    handler = partial(WebHandler, directory=str(args.directory.resolve()))
    server = ThreadingHTTPServer(('127.0.0.1', args.port), handler)
    print(f'Uninvited preview: http://127.0.0.1:{args.port}/', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == '__main__':
    main()
