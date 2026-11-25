"""Verify the candidate's role and project boundaries on an isolated local API."""
import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--url', default='http://localhost:5184/api/v1')
parser.add_argument('--access-file', default='.local/access.json')
parser.add_argument('--fixture-file', default='.local/verify-evidence-files-candidate.json')
parser.add_argument('--output', default='.local/role-results.json')
parser.add_argument('--wait-for-auth-window', action='store_true')
parser.add_argument('--allow-local-writes', action='store_true', required=True)
args = parser.parse_args()
url = urllib.parse.urlparse(args.url)
if url.hostname not in ('localhost', '127.0.0.1') or url.path != '/api/v1' or url.query or url.fragment:
    raise SystemExit('Use a local isolated API at /api/v1.')
if args.wait_for_auth_window:
    print('Waiting for the authentication window; rate limiting remains enabled.', flush=True)
    time.sleep(62)

password = json.loads(Path(args.access_file).read_text(encoding='utf-8-sig'))['password']
checks = []
tokens = {}


def request(method, path, body=None, token=None, status=200, role='setup'):
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(args.url + path, method=method, headers=headers,
                                 data=None if body is None else json.dumps(body).encode())
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            code, data = response.status, response.read()
    except urllib.error.HTTPError as error:
        code, data = error.code, error.read()
    expected = list(status) if isinstance(status, tuple) else [status]
    assert code in expected, f'{role}: {method} {path}: expected{expected}, got{code}'
    checks.append({'role': role, 'method': method, 'path': path, 'expected': expected, 'actual': code})
    if data.startswith((b'%PDF-', b'\x89PNG')):
        return data
    return json.loads(data) if data else None


for role in ('admin', 'warehouse', 'site'):
    tokens[role] = request('POST', '/authentication/sign-in',
                           {'username': role + '.local', 'password': password})['accessToken']
outsider = 'role.outsider.' + uuid.uuid4().hex[:8]
request('POST', '/authentication/sign-up', {'username': outsider, 'password': password})
tokens['outsider'] = request('POST', '/authentication/sign-in',
                             {'username': outsider, 'password': password})['accessToken']
roles = ('admin', 'warehouse', 'site', 'outsider', 'anonymous')
expected_roles = {'admin': 'ADMIN', 'warehouse': 'WAREHOUSE_MANAGER', 'site': 'SITE_MANAGER', 'outsider': 'SITE_MANAGER'}
for role, token in tokens.items():
    me = request('GET', '/users/me', token=token, role=role)
    assert me['role'] == expected_roles[role]
request('GET', '/users/me', token='invalid-local-test-token', status=401, role='invalid-token')

# Expected order is admin, warehouse, assigned resident, unassigned resident, anonymous.
matrix = {
    '/users': (200, 403, 403, 403, 401),
    '/materials': (200, 200, 403, 403, 401),
    '/vehicles': (200, 200, 403, 403, 401),
    '/geofences': (200, 200, 403, 403, 401),
    '/dispatches': (200, 200, 403, 403, 401),
    '/orders': (200, 200, 200, 200, 401),
    '/projects': (200, 200, 200, 200, 401),
    '/receptions': (200, 403, 200, 200, 401),
    '/upcoming-arrivals': (200, 403, 200, 200, 401),
    '/discrepancy-cases': (200, 403, 200, 200, 401),
    '/activity-records': (200, 200, 200, 200, 401),
    '/routes': (200, 200, 200, 200, 401),
}
for path, statuses in matrix.items():
    for role, status in zip(roles, statuses):
        request('GET', path, token=tokens.get(role), status=status, role=role)

catalog = request('GET', '/orders/request-catalog', token=tokens['site'], role='site')
project = catalog[0]
assert request('GET', '/orders/request-catalog', token=tokens['outsider'], role='outsider') == []
order = request('POST', '/orders/requests', {
    'projectId': project['id'], 'priority': 'AVERAGE',
    'date': dt.datetime.now(dt.timezone.utc).date().isoformat(),
    'items': [{'materialId': project['materials'][0]['id'], 'quantity': 4}],
}, token=tokens['site'], status=201, role='site')
for role, status in zip(roles, (200, 200, 200, 404, 401)):
    request('GET', f"/orders/{order['id']}", token=tokens.get(role), status=status, role=role)
request('PATCH', f"/orders/{order['id']}/status", {'status': 'APPROVED'},
        token=tokens['site'], status=403, role='site')

fixture = json.loads(Path(args.fixture_file).read_text(encoding='utf-8'))
rid, cid, photo = fixture['receptionId'], fixture['caseId'], fixture['storedPhoto']
for path in [f'/receptions/{rid}', f'/receptions/{rid}/evidence-context',
             f'/discrepancy-cases/{cid}', f'/discrepancy-cases/{cid}/timeline',
             f'/discrepancy-cases/{cid}/report']:
    for role, status in zip(roles, (200, 403, 200, 404, 401)):
        body = request('GET', path, token=tokens.get(role), status=status, role=role)
        if status == 200 and path.endswith('/report'):
            assert body.startswith(b'%PDF-') and b'%%EOF' in body
for role, status in zip(roles, (200, 403, 200, 404, 401)):
    body = request('GET', photo['fileUrl'].removeprefix('/api/v1'),
                   token=tokens.get(role), status=status, role=role)
    if status == 200:
        assert hashlib.sha256(body).hexdigest() == photo['sha256'].lower()

origin = urllib.parse.urlunparse((url.scheme, url.netloc, '', '', '', ''))
with urllib.request.urlopen(origin + '/openapi/v1.json', timeout=30) as response:
    document = json.load(response)
checks.append({'role': 'public-documentation', 'method': 'GET', 'path': '/openapi/v1.json', 'expected': [200], 'actual': 200})
protected_operations = 0
for path, operations in document['paths'].items():
    for method, operation in operations.items():
        if method not in ('get', 'post', 'put', 'patch', 'delete'):
            continue
        if path.startswith('/api/v1/authentication/') or path == '/health':
            assert not operation.get('security'), (method, path)
        elif path.startswith('/api/v1/'):
            assert {'Bearer': []} in operation.get('security', []), (method, path)
            protected_operations += 1

report = {
    'scope': 'Local isolated database. Anonymous, administrator, warehouse, assigned and unassigned resident boundaries.',
    'httpChecks': len(checks), 'checks': checks, 'protectedOpenApiOperations': protected_operations,
    'fixtures': {'orderId': order['id'], 'receptionId': rid, 'caseId': cid, 'photoId': photo['id']},
    'tokensIncluded': False, 'publicEndpointsRemainAnonymous': True,
}
Path(args.output).write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(f'PASS: {len(checks)} role HTTP checks; {protected_operations} protected OpenAPI operations declare Bearer. No tokens in report.')
