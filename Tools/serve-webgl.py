"""Serve WebGL and proxy local authentication/API routes without changing the save origin."""
import argparse
from functools import partial
from http.client import HTTPConnection
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlsplit


class Handler(SimpleHTTPRequestHandler):
    """Keep game assets, session cookies, and backend requests on one browser origin."""

    extensions_map = {
        **SimpleHTTPRequestHandler.extensions_map,
        '.wasm': 'application/wasm',
        '.data': 'application/octet-stream',
    }

    def end_headers(self):
        """Prevent stale local build assets from masking development changes."""
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

    def do_GET(self):
        """Proxy backend GET routes or serve the existing WebGL build."""
        if self.is_backend_route():
            self.proxy_backend()
        else:
            super().do_GET()

    def do_POST(self):
        """Forward logout requests without following redirects or merging cookies."""
        self.proxy_backend()

    def do_PUT(self):
        """Forward authenticated save writes with their CSRF headers."""
        self.proxy_backend()

    def is_backend_route(self):
        """Restrict proxying to the backend's explicit local route prefixes."""
        path = urlsplit(self.path).path
        return path.startswith(('/api/', '/auth/')) or path in ('/signin-facebook', '/signin-discord')

    def proxy_backend(self):
        """Relay bounded requests to the fixed local API, preserving the browser-facing Host."""
        if not self.is_backend_route():
            self.send_error(404)
            return
        if self.headers.get('Transfer-Encoding'):
            self.send_error(400, 'Chunked requests are not supported by the local proxy.')
            return
        try:
            length = int(self.headers.get('Content-Length', '0'))
        except ValueError:
            self.send_error(400)
            return
        if length < 0 or length > 350 * 1024:
            self.send_error(413)
            return
        body = self.rfile.read(length) if length else None
        excluded = {'connection', 'transfer-encoding', 'keep-alive', 'upgrade', 'proxy-connection'}
        headers = {key: value for key, value in self.headers.items() if key.lower() not in excluded}
        connection = HTTPConnection('127.0.0.1', 5100, timeout=15)
        try:
            connection.request(self.command, self.path, body=body, headers=headers)
            response = connection.getresponse()
            content = response.read()
            self.send_response(response.status)
            for key, value in response.getheaders():
                if key.lower() not in excluded | {'content-length'}:
                    self.send_header(key, value)
            self.send_header('Content-Length', str(len(content)))
            self.end_headers()
            self.wfile.write(content)
        except OSError:
            self.send_error(502, 'Start the .NET backend on localhost:5100.')
        finally:
            connection.close()


def main():
    """Start the loopback-only development server after checking the build directory."""
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8080)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1] / 'Builds' / 'WebGL'
    if not (root / 'index.html').exists():
        raise SystemExit('No WebGL build found. Run Tools/build-webgl.ps1 first.')
    print(f'Little Colony: http://localhost:{args.port}', flush=True)
    ThreadingHTTPServer(('127.0.0.1', args.port), partial(Handler, directory=str(root))).serve_forever()


if __name__ == '__main__':
    main()
