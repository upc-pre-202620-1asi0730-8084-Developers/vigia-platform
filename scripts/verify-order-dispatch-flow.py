import argparse
import datetime as dt
import json
import pathlib
import urllib.error
import urllib.parse
import urllib.request
import uuid

parser = argparse.ArgumentParser(description='Verify resident request through dispatch weighing on an isolated local API.')
parser.add_argument('--url', default='http://127.0.0.1:5145/api/v1')
parser.add_argument('--access-file', default=str(pathlib.Path(__file__).resolve().parents[1] / '.local/access.json'))
parser.add_argument('--allow-local-writes', action='store_true', required=True)
args = parser.parse_args()
if urllib.parse.urlparse(args.url).hostname not in ('localhost', '127.0.0.1'):
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
        with urllib.request.urlopen(req, timeout=20) as response:
            status, content = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, content = error.code, error.read()
    assert status == expected, f'{method} {path}: expected {expected}, got {status}: {content[:300]!r}'
    checks += 1
    return json.loads(content) if content else None


tokens = {role: request('POST', '/authentication/sign-in', {'username': role+'.local', 'password': password})['accessToken']
          for role in ('site', 'warehouse')}
site, warehouse = tokens['site'], tokens['warehouse']
catalog = request('GET', '/orders/request-catalog', token=site)
project = next(project for project in catalog if project['materials'])
material = project['materials'][0]
order = request('POST', '/orders/requests', {
    'projectId': project['id'], 'date': dt.datetime.now(dt.timezone.utc).date().isoformat(), 'priority': 'AVERAGE',
    'items': [{'materialId': material['id'], 'quantity': 3}], 'observations': 'Local order-to-weighing flow'
}, site, 201)
assert order['status'] == 'PENDING'
order_path = f"/orders/{order['id']}"
plan = {'projectName': project['name'], 'relatedOrderId': str(order['id']),
        'departureDate': dt.datetime.now(dt.timezone.utc).date().isoformat(), 'estimatedTime': '18:00',
        'dispatchType': 'DEPARTURE_TO_WORK', 'priority': 'AVERAGE', 'observations': 'Local integrated flow'}
request('POST', '/dispatches/from-order', plan, site, 403)
request('POST', '/dispatches/from-order', plan, warehouse, 409)
request('PATCH', order_path+'/status', {'status': 'UNDER_REVIEW', 'performedBy': 'Ignored client actor'}, warehouse)
assert request('GET', order_path, token=site)['status'] == 'UNDER_REVIEW'
request('PATCH', order_path+'/status', {'status': 'APPROVED', 'performedBy': 'Ignored client actor'}, warehouse)
assert request('GET', order_path, token=site)['status'] == 'APPROVED'
dispatch = request('POST', '/dispatches/from-order', plan, warehouse, 201)
assert dispatch['status'] == 'IN_PREPARATION' and str(dispatch['relatedOrderId']) == str(order['id'])
assert [(i['material'], i['quantity'], i['unit']) for i in dispatch['items']] == [
    (i['material'], i['quantity'], i['unit']) for i in order['items']]
path = f"/dispatches/{dispatch['id']}"
weigh_path = path+'/weighing'
measurement = {'weightKg': 100.001, 'expectedWeightKg': 100, 'toleranceKg': 0.001, 'requestId': str(uuid.uuid4())}
request('POST', weigh_path, measurement, expected=401)
request('POST', weigh_path, measurement, site, 403)
request('GET', weigh_path, token=site, expected=403)
request('POST', weigh_path, measurement, warehouse, 409)
assert request('GET', weigh_path, token=warehouse) == []
vehicle = request('GET', '/vehicles', token=warehouse)[0]
request('PUT', path+'/vehicle', {'vehicleId': vehicle['id'], 'vehiclePlate': vehicle['licensePlate'], 'performedBy': 'Current user'}, warehouse)
first = request('POST', weigh_path, measurement, warehouse, 201)
assert first['comparisonStatus'] == 'WITHIN_TOLERANCE' and not first['requiresReview']
assert first['weightBasis'] == 'NET' and first['weightKg'] != order['items'][0]['quantity']
replayed = request('POST', weigh_path, measurement, warehouse, 201)
assert replayed['id'] == first['id'] and len(request('GET', weigh_path, token=warehouse)) == 1
assert not request('GET', path, token=warehouse)['requiresWeightReview']
outside = request('POST', weigh_path, {**measurement, 'weightKg': 100.002, 'requestId': str(uuid.uuid4())}, warehouse, 201)
assert outside['requiresReview'] and outside['comparisonStatus'] == 'OUTSIDE_TOLERANCE'
assert request('GET', path, token=warehouse)['requiresWeightReview']
request('POST', path+'/items', {'code': 'MAT-LOCAL-FLOW', 'material': 'Local additional material', 'quantity': 1,
        'unit': 'kg', 'performedBy': 'Current user'}, warehouse)
historical = request('GET', weigh_path, token=warehouse)
assert len(historical) == 2 and all(not item['isCurrent'] and item['requiresReview'] for item in historical)
final_body = {**measurement, 'requestId': str(uuid.uuid4())}
final = request('POST', weigh_path, final_body, warehouse, 201)
assert final['isCurrent'] and not final['requiresReview']
request('POST', path+'/cancellation', {'reason': 'Completed isolated integration check', 'performedBy': 'Current user'}, warehouse)
request('POST', weigh_path, {**measurement, 'requestId': str(uuid.uuid4())}, warehouse, 409)
assert request('POST', weigh_path, final_body, warehouse, 201)['id'] == final['id']
assert len(request('GET', weigh_path, token=warehouse)) == 3
request('GET', '/dispatches/2147483647/weighing', token=warehouse, expected=404)
assert request('GET', order_path, token=site)['status'] == 'APPROVED'
print(f'PASS: {checks} HTTP checks; resident request -> warehouse review -> dispatch with original items -> '
      'vehicle -> net weighing, inclusive/outside tolerance, history, changed load, cancellation and safe replay. '
      'Guide/departure authorization remains outside this check.')
