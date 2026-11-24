import argparse
from concurrent.futures import ThreadPoolExecutor
import datetime as dt
import json
import pathlib
import secrets
import threading
import urllib.error
import urllib.parse
import urllib.request
import uuid

parser = argparse.ArgumentParser(description='Verify dispatch weighing against an isolated local Vigia API.')
parser.add_argument('--url', default='http://127.0.0.1:5145/api/v1')
parser.add_argument('--access-file', default=str(pathlib.Path(__file__).resolve().parents[1] / '.local/access.json'))
parser.add_argument('--allow-local-writes', action='store_true', required=True)
args = parser.parse_args()
if urllib.parse.urlparse(args.url).hostname not in ('localhost', '127.0.0.1'):
    raise SystemExit('Only an isolated local API is supported.')
password = json.loads(pathlib.Path(args.access_file).read_text(encoding='utf-8-sig'))['password']
checks = 0
lock = threading.Lock()


def request(method, path, body=None, token=None, expected=200, location=False):
    global checks
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(args.url + path, method=method, headers=headers,
                                 data=json.dumps(body).encode() if body is not None else None)
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            status, content = response.status, response.read()
            resource_location = response.headers.get('Location')
    except urllib.error.HTTPError as error:
        status, content, resource_location = error.code, error.read(), None
    allowed = expected if isinstance(expected, tuple) else (expected,)
    assert status in allowed, f'{method} {path}: expected {allowed}, got {status}: {content[:400]!r}'
    with lock:
        checks += 1
    payload = json.loads(content) if content else None
    return (payload, resource_location) if location else payload


tokens = {role: request('POST', '/authentication/sign-in', {'username': role + '.local', 'password': password})['accessToken']
          for role in ('admin', 'warehouse', 'site')}
admin, warehouse, site = (tokens[role] for role in ('admin', 'warehouse', 'site'))
orders = request('GET', '/orders', token=warehouse)
approved = next(order for order in orders if order['status'] == 'APPROVED')
vehicle = request('GET', '/vehicles', token=warehouse)[0]
plan = {'projectName': approved['projectName'], 'relatedOrderId': str(approved['id']),
        'departureDate': dt.datetime.now(dt.timezone.utc).date().isoformat(), 'estimatedTime': '18:00',
        'dispatchType': 'DEPARTURE_TO_WORK', 'priority': 'AVERAGE', 'observations': 'Local weighing checks'}
dispatch = request('POST', '/dispatches/from-order', plan, warehouse, 201)
path = f"/dispatches/{dispatch['id']}"
weighing_path = path + '/weighing'
valid = {'weightKg': 100, 'expectedWeightKg': 100, 'toleranceKg': 2, 'requestId': str(uuid.uuid4()),
         'recordedByAccountId': 999999, 'recordedBy': 'Spoofed actor', 'weighedAt': '2000-01-01T00:00:00Z'}
request('POST', weighing_path, valid, expected=401)
request('POST', weighing_path, valid, site, 403)
request('GET', weighing_path, token=site, expected=403)
request('POST', '/dispatches/2147483647/weighing', valid, warehouse, 404)
request('GET', '/dispatches/2147483647/weighing', token=warehouse, expected=404)
request('POST', weighing_path, valid, warehouse, 409)
assert request('GET', weighing_path, token=warehouse) == []
assert request('GET', path, token=warehouse)['requiresWeightReview'] is True
request('PUT', path + '/vehicle', {'vehicleId': vehicle['id'], 'vehiclePlate': vehicle['licensePlate'],
        'performedBy': 'Spoofed actor'}, warehouse)
for invalid in (
    {'weightKg': 0}, {'weightKg': -1}, {'weightKg': 100.0001}, {'weightKg': 1000000000},
    {'expectedWeightKg': 0}, {'toleranceKg': -1}, {'toleranceKg': 0.0001},
    {'expectedWeightKg': None}, {'toleranceKg': None}, {'requestId': str(uuid.UUID(int=0))}
):
    request('POST', weighing_path, {**valid, **invalid}, warehouse, 400)
assert request('GET', weighing_path, token=warehouse) == []
activity_before = len(request('GET', '/activity-records', token=admin))
timeline_before = len(request('GET', path, token=warehouse)['timeline'])
first, resource_location = request('POST', weighing_path, valid, warehouse, 201, location=True)
assert first['weightBasis'] == 'NET' and first['comparisonStatus'] == 'WITHIN_TOLERANCE'
assert first['differenceKg'] == 0 and not first['requiresReview'] and first['isCurrent']
assert first['recordedBy'] == 'Local Warehouse Manager' and first['recordedByAccountId'] != 999999
assert dt.datetime.fromisoformat(first['weighedAt'].replace('Z', '+00:00')).year > 2000
assert resource_location.endswith(f"{weighing_path}/{first['id']}")
assert request('GET', f"{weighing_path}/{first['id']}", token=warehouse)['id'] == first['id']
assert len(request('GET', '/activity-records', token=admin)) == activity_before + 1
assert len(request('GET', path, token=warehouse)['timeline']) == timeline_before + 1
assert request('POST', weighing_path, valid, warehouse, 201)['id'] == first['id']
assert len(request('GET', weighing_path, token=warehouse)) == 1
assert len(request('GET', '/activity-records', token=admin)) == activity_before + 1
request('POST', weighing_path, {**valid, 'weightKg': 101}, warehouse, 409)
for actual, expected, tolerance, outcome in (
    (98, 100, 2, 'WITHIN_TOLERANCE'), (102, 100, 2, 'WITHIN_TOLERANCE'),
    (97.999, 100, 2, 'OUTSIDE_TOLERANCE'), (102.001, 100, 2, 'OUTSIDE_TOLERANCE'),
    (100.001, 100, 0.001, 'WITHIN_TOLERANCE'), (100, 100, 0, 'WITHIN_TOLERANCE'),
    (100.001, 100, 0, 'OUTSIDE_TOLERANCE')
):
    result = request('POST', weighing_path, {'weightKg': actual, 'expectedWeightKg': expected,
                     'toleranceKg': tolerance, 'requestId': str(uuid.uuid4())}, warehouse, 201)
    assert result['comparisonStatus'] == outcome
    assert result['requiresReview'] == (outcome == 'OUTSIDE_TOLERANCE')
    detail = request('GET', path, token=warehouse)
    assert detail['latestWeighing']['id'] == result['id']
    assert detail['requiresWeightReview'] == result['requiresReview']
minimal = request('POST', weighing_path, {'weightKg': 100}, warehouse, 201)
assert minimal['comparisonStatus'] == 'NOT_EVALUATED' and minimal['requiresReview'] and minimal['differenceKg'] is None
complete = request('POST', weighing_path, {**valid, 'requestId': str(uuid.uuid4())}, warehouse, 201)
request('POST', path + '/items', {'code': 'MAT-9998', 'material': 'Local test aggregate', 'quantity': 3,
        'unit': 'kg', 'observation': None, 'performedBy': 'Spoofed actor'}, warehouse)
changed = request('GET', path, token=warehouse)
assert changed['requiresWeightReview'] and not changed['latestWeighing']['isCurrent']
historical = request('GET', f"{weighing_path}/{complete['id']}", token=warehouse)
assert historical['weightKg'] == 100 and historical['comparisonStatus'] == 'WITHIN_TOLERANCE'
assert not historical['isCurrent'] and historical['requiresReview']
plate = ''.join(secrets.choice('ABCDEFGHIJKLMNOPQRSTUVWXYZ') for _ in range(3)) + '-' + str(secrets.randbelow(900) + 100)
new_vehicle = request('POST', '/vehicles', {'companyId': 'vigia', 'licensePlate': plate, 'brand': 'Local',
                      'model': 'Weighing check', 'maxCapacityTons': 30, 'gpsDeviceId': 'GPS-WEIGH-' + secrets.token_hex(4)}, warehouse, 201)
request('POST', weighing_path, {**valid, 'requestId': str(uuid.uuid4())}, warehouse, 201)
request('PUT', path + '/vehicle', {'vehicleId': new_vehicle['id'], 'vehiclePlate': plate,
        'performedBy': 'Spoofed actor'}, warehouse)
assert request('GET', path, token=warehouse)['requiresWeightReview'] is True
retry_body = {**valid, 'requestId': str(uuid.uuid4())}
final = request('POST', weighing_path, retry_body, warehouse, 201)
assert not final['requiresReview'] and final['vehicleId'] == new_vehicle['id']
request('PATCH', path + '/status', {'status': 'IN_TRANSIT', 'performedBy': 'Spoofed actor'}, warehouse, 409)
request('POST', path + '/delivery-guide', {'driverName': 'Local Driver', 'carrierName': 'Local Carrier'}, warehouse, 201)
request('PATCH', path + '/status', {'status': 'IN_TRANSIT', 'performedBy': 'Spoofed actor'}, warehouse)
request('POST', weighing_path, {**valid, 'requestId': str(uuid.uuid4())}, warehouse, 409)
assert request('POST', weighing_path, retry_body, warehouse, 201)['id'] == final['id']
assert request('GET', path, token=warehouse)['latestWeighing']['isCurrent'] is True
request('PATCH', path + '/status', {'status': 'COMPLETED', 'performedBy': 'Spoofed actor'}, warehouse)
request('POST', weighing_path, {'weightKg': 100}, warehouse, 409)
other = request('POST', '/dispatches/from-order', plan, warehouse, 201)
other_path = f"/dispatches/{other['id']}"
request('PUT', other_path + '/vehicle', {'vehicleId': vehicle['id'], 'vehiclePlate': vehicle['licensePlate'],
        'performedBy': 'Spoofed actor'}, warehouse)
race_body = {**valid, 'requestId': str(uuid.uuid4())}
with ThreadPoolExecutor(max_workers=2) as pool:
    list(pool.map(lambda _: request('POST', other_path + '/weighing', race_body, warehouse, (201, 409)), range(2)))
race = request('POST', other_path + '/weighing', race_body, warehouse, 201)
assert len(request('GET', other_path + '/weighing', token=warehouse)) == 1
request('GET', other_path + f"/weighing/{first['id']}", token=warehouse, expected=404)
request('POST', other_path + '/cancellation', {'reason': 'Local check', 'performedBy': 'Spoofed actor'}, warehouse)
request('POST', other_path + '/weighing', {'weightKg': 100}, warehouse, 409)
print(f'PASS: {checks} HTTP checks; net kg, inclusive tolerance, precision, authorization, '
      'audit, immutable history, load/vehicle invalidation, replay and concurrent duplicate protection.')
