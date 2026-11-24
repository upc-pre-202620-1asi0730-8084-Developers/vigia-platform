import argparse
import json
import pathlib
import secrets
import urllib.error
import urllib.parse
import urllib.request

parser = argparse.ArgumentParser(description='Verify resident requests against an isolated local Vigia API.')
parser.add_argument('--url', default='http://127.0.0.1:5135/api/v1')
parser.add_argument('--access-file', default=str(pathlib.Path(__file__).resolve().parents[1] / '.local/access.json'))
parser.add_argument('--allow-local-writes', action='store_true', required=True)
args = parser.parse_args()
if urllib.parse.urlparse(args.url).hostname not in ('127.0.0.1', 'localhost'):
    raise SystemExit('Only an isolated local API is supported.')
password = json.loads(pathlib.Path(args.access_file).read_text(encoding='utf-8-sig'))['password']
checks = 0


def request(method, path, body=None, token=None, expected=200):
    global checks
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(args.url + path, method=method, headers=headers,
                                 data=json.dumps(body).encode() if body is not None else None)
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            status, content = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, content = error.code, error.read()
    assert status == expected, f'{method} {path}: expected {expected}, got {status}: {content[:300]!r}'
    checks += 1
    return json.loads(content) if content else None


request('GET', '/orders/request-catalog', expected=401)
tokens = {role: request('POST', '/authentication/sign-in',
                       {'username': role + '.local', 'password': password})['accessToken']
          for role in ('site', 'warehouse', 'admin')}
site, warehouse, admin = (tokens[role] for role in ('site', 'warehouse', 'admin'))
catalog = request('GET', '/orders/request-catalog', token=site)
assert catalog and catalog[0]['materials']
project, material = catalog[0], catalog[0]['materials'][0]
assert 'currentStock' not in material and 'minimumStock' not in material
request('GET', '/orders/request-catalog', token=warehouse, expected=403)
request('GET', '/materials', token=site, expected=403)
legacy = request('GET', '/orders', token=warehouse)
legacy_id = next(order['id'] for order in legacy if order['code'] in ('SOL-101', 'SOL-102', 'SOL-103'))
request('GET', f'/orders/{legacy_id}', token=site, expected=404)
profile = request('GET', '/users/me', token=site)
payload = {'projectId': project['id'], 'priority': 'AVERAGE', 'date': '2026-11-24',
           'items': [{'materialId': material['id'], 'quantity': 2, 'observation': None}],
           'observations': 'Resident request integration check', 'requesterName': 'Spoofed user',
           'requesterAccountId': 999999, 'projectName': 'Spoofed project', 'status': 'APPROVED'}
request('POST', '/orders/requests', {**payload, 'projectId': 2147483647}, site, 403)
request('POST', '/orders/requests', {**payload, 'items': [{'materialId': 2147483647, 'quantity': 1}]}, site, 400)
request('POST', '/orders/requests', {**payload, 'items': [{'materialId': material['id'], 'quantity': 0}]}, site, 400)
request('POST', '/orders/requests', {**payload, 'items': payload['items'] * 2}, site, 400)
request('POST', '/orders/requests', {**payload, 'items': [None]}, site, 400)
request('POST', '/orders/requests', {**payload, 'items': []}, site, 400)
request('POST', '/orders/requests', {**payload, 'priority': '999'}, site, 400)
request('POST', '/orders/requests', {**payload, 'observations': 'x' * 351}, site, 400)
request('POST', '/orders/requests', payload, warehouse, 403)
created = request('POST', '/orders/requests', payload, site, 201)
assert created['status'] == 'PENDING' and created['requesterName'] == profile['fullName']
assert created['projectName'] == project['name'] and created['items'][0]['material'] == material['name']
assert created['items'][0]['unit'] == material['unit'] and created['code'].startswith('SOL-')
assert any(order['id'] == created['id'] for order in request('GET', '/orders?status=PENDING', token=warehouse))
path = f"/orders/{created['id']}"
assert request('GET', path, token=site)['id'] == created['id']
assert any(order['id'] == created['id'] for order in request('GET', '/orders?status=PENDING', token=site))
request('POST', '/orders', {}, site, 403)
request('PATCH', path + '/status', {'status': 'APPROVED'}, site, 403)
request('POST', path + '/attached-files', {}, site, 403)
other_name = 'resident.' + secrets.token_hex(4)
request('POST', '/authentication/sign-up', {'username': other_name, 'password': password})
other = request('POST', '/authentication/sign-in', {'username': other_name, 'password': password})['accessToken']
other_profile = request('GET', '/users/me', token=other)
request('PUT', f"/users/{other_profile['id']}", {'fullName': profile['fullName'],
        'email': 'other@example.test', 'phone': '', 'timezone': 'America/Lima'}, other)
assert request('GET', '/orders/request-catalog', token=other) == []
assert request('GET', '/orders', token=other) == []
assert request('GET', '/orders?code=' + created['code'], token=other) == []
request('GET', path, token=other, expected=404)
request('POST', '/orders/requests', payload, other, 403)
request('PATCH', path + '/status', {'status': 'APPROVED', 'performedBy': 'Spoofed user'}, warehouse, 409)
request('PATCH', path + '/status', {'status': 'UNDER_REVIEW', 'performedBy': 'Spoofed user'}, warehouse)
reviewed = request('GET', path, token=site)
assert reviewed['status'] == 'UNDER_REVIEW' and reviewed['timeline'][-1]['user'] == 'Local Warehouse Manager'
request('PATCH', path + '/status', {'status': 'APPROVED', 'performedBy': 'Spoofed user'}, warehouse)
assert request('GET', path, token=site)['status'] == 'APPROVED'
assert any(order['id'] == created['id'] for order in request('GET', '/orders', token=admin))
assert any(order['id'] == created['id'] for order in request('GET', '/orders', token=warehouse))
print(f'PASS: {checks} HTTP checks; assigned catalog, resident submission, ownership despite equal names, '
      'spoofed fields, invalid inputs, warehouse review and legacy isolation.')
