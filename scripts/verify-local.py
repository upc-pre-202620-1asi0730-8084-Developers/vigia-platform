import argparse
import datetime as dt
import json
import pathlib
import secrets
import urllib.error
import urllib.parse
import urllib.request

parser = argparse.ArgumentParser(description='Exercise the real API against the isolated local demo database.')
parser.add_argument('--url', default='http://localhost:5125/api/v1')
parser.add_argument('--access-file', default=str(pathlib.Path(__file__).resolve().parents[1] / '.local/access.json'))
parser.add_argument('--allow-local-writes', action='store_true', required=True)
args = parser.parse_args()
if urllib.parse.urlparse(args.url).hostname not in ('localhost', '127.0.0.1'):
    raise SystemExit('This verification script only supports localhost.')
password = json.loads(pathlib.Path(args.access_file).read_text(encoding='utf-8-sig'))['password']
checks = 0


def request(method, path, body=None, token=None, status=200):
    global checks
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(args.url + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            code, payload = response.status, response.read()
    except urllib.error.HTTPError as error:
        code, payload = error.code, error.read()
    assert code == status, f'{method} {path}: expected {status}, got {code}: {payload[:500]!r}'
    checks += 1
    if not payload:
        return None
    if payload.startswith(b'%PDF-'):
        return payload
    try:
        return json.loads(payload)
    except json.JSONDecodeError:
        return payload.decode()


request('GET', '/projects', status=401)
request('POST', '/authentication/sign-in', {'username': 'admin.local', 'password': 'incorrect-password'}, status=401)
tokens = {role: request('POST', '/authentication/sign-in', {'username': role + '.local', 'password': password})['accessToken']
          for role in ('admin', 'warehouse', 'site')}
admin, warehouse, site = (tokens[k] for k in ('admin', 'warehouse', 'site'))
profile = request('GET', '/users/me', token=site)
assert profile['role'] == 'SITE_MANAGER' and profile['allowedRoles'] == ['SITE_MANAGER']
request('GET', '/users', token=site, status=403)
request('GET', '/materials', token=site, status=403)
request('GET', '/receptions', token=warehouse, status=403)
request('GET', '/users/1', token=site, status=403)
request('PUT', f"/users/{profile['id']}", {**profile, 'role': 'ADMIN', 'fullName': 'Local Site Resident',
        'email': 'resident@example.test', 'phone': '999888777', 'timezone': 'America/Lima'}, token=site)
assert request('GET', '/users/me', token=site)['role'] == 'SITE_MANAGER'
username = 'check.' + secrets.token_hex(4)
request('POST', '/authentication/sign-up', {'username': username, 'password': password})
request('POST', '/authentication/sign-up', {'username': username, 'password': password}, status=409)
new_token = request('POST', '/authentication/sign-in', {'username': username, 'password': password})['accessToken']
assert request('GET', '/users/me', token=new_token)['role'] == 'SITE_MANAGER'
for path in ('/projects', '/materials', '/inventory-movements', '/activity-records', '/vehicles', '/geofences', '/routes', '/orders', '/dispatches'):
    assert isinstance(request('GET', path, token=admin), list), path
projects = request('GET', '/projects', token=admin)
orders = request('GET', '/orders', token=warehouse)
approved = next(o for o in orders if o['status'] == 'APPROVED')
pending = next((o for o in orders if o['status'] == 'PENDING'), None)
if pending:
    order_path = f"/orders/{pending['id']}/status"
    request('PATCH', order_path, {'status': 'APPROVED', 'performedBy': 'spoofed'}, warehouse, 409)
    request('PATCH', order_path, {'status': 'UNDER_REVIEW', 'performedBy': 'spoofed'}, warehouse)
    result = request('PATCH', order_path, {'status': 'APPROVED', 'performedBy': 'spoofed'}, warehouse)
    assert result['timeline'][-1]['user'] == 'Local Warehouse Manager'
vehicle = request('GET', '/vehicles', token=warehouse)[0]
unpaired = request('POST', '/telemetry/reports', {'gpsDeviceId': 'UNPAIRED-LOCAL', 'latitude': -12.08, 'longitude': -77.03}, warehouse)
assert unpaired['recorded'] is True and unpaired['accepted'] is False
request('POST', '/telemetry/reports', {'gpsDeviceId': vehicle['gpsDeviceId'], 'latitude': -12.08, 'longitude': -77.03}, warehouse)
request('POST', '/telemetry/reports', {'gpsDeviceId': vehicle['gpsDeviceId'], 'latitude': 200, 'longitude': -77.03}, warehouse, 400)
route = request('GET', '/routes', token=warehouse)[0]
now = dt.datetime.now(dt.timezone.utc)
position = {'latitude': -12.09, 'longitude': -77.02, 'speedKmh': 30, 'reportedAt': now.isoformat()}
request('POST', f"/routes/{route['id']}/positions", position, warehouse)
request('POST', f"/routes/{route['id']}/positions", {**position, 'reportedAt': (now - dt.timedelta(days=1)).isoformat()}, warehouse, 400)
request('POST', f"/routes/{route['id']}/signal-lost", {'detectedAt': (now + dt.timedelta(seconds=1)).isoformat()}, warehouse)

for discrepant in (False, True):
    plan = {'projectName': approved['projectName'], 'relatedOrderId': str(approved['id']),
            'departureDate': now.date().isoformat(), 'estimatedTime': '18:00',
            'dispatchType': 'DEPARTURE_TO_WORK', 'priority': 'AVERAGE', 'observations': 'HTTP integration verification'}
    dispatch = request('POST', '/dispatches/from-order', plan, warehouse, 201)
    dispatch_path = f"/dispatches/{dispatch['id']}"
    request('PATCH', dispatch_path + '/status', {'status': 'IN_TRANSIT', 'performedBy': 'spoofed'}, warehouse, 409)
    request('PUT', dispatch_path + '/vehicle', {'vehicleId': 999999, 'vehiclePlate': 'ABC-123', 'performedBy': 'spoofed'}, warehouse, 400)
    request('PUT', dispatch_path + '/vehicle', {'vehicleId': vehicle['id'], 'vehiclePlate': vehicle['licensePlate'], 'performedBy': 'spoofed'}, warehouse)
    request('PATCH', dispatch_path + '/status', {'status': 'IN_TRANSIT', 'performedBy': 'spoofed'}, warehouse)
    arrivals = request('GET', '/upcoming-arrivals', token=site)
    assert any(str(a['dispatchId']) == str(dispatch['id']) for a in arrivals)
    checklist = {'materialGoodCondition': True, 'quantityVerified': True, 'deliveryGuideReceived': True,
                 'photographicEvidence': False, 'observationsRecorded': discrepant}
    arrival = {'dispatchId': str(dispatch['id']), 'deliveryGuideNumber': 'GR-LOCAL-' + secrets.token_hex(3),
               'driverName': 'Local Driver', 'checklist': checklist, 'observations': 'Difference found' if discrepant else '',
               'items': [{'materialName': item['material'], 'receivedQuantity': item['quantity'] - (1 if discrepant else 0)} for item in dispatch['items']]}
    reception = request('POST', '/receptions/physical', arrival, site, 201)
    assert reception['digitalSignature'] is None and reception['scaleWeight'] is None
    assert reception['verifiedByUserId'] == str(profile['id'])
    request('POST', '/receptions/physical', arrival, site, 409)
    request('PUT', f"/receptions/{reception['id']}", arrival, site)
    request('POST', f"/receptions/{reception['id']}/compare", {'items': []}, site, 400)
    comparison = request('POST', f"/receptions/{reception['id']}/compare", {'items': [
        {'id': item['id'], 'receivedQuantity': item['receivedQuantity']} for item in reception['items']]}, site)
    assert comparison['hasDiscrepancy'] == discrepant
    verify = {'status': 'VERIFIED_DISCREPANT' if discrepant else 'VERIFIED_CONFORMANT', 'checklist': checklist,
              'observations': arrival['observations']}
    if discrepant:
        request('POST', f"/receptions/{reception['id']}/evidence", {'evidenceType': 'PHOTO',
                'fileUrl': 'https://example.test/evidence.jpg', 'description': 'Local verification evidence reference'}, site)
    result = request('POST', f"/receptions/{reception['id']}/verify", verify, site)
    assert result['status'] == ('DiscrepancyReported' if discrepant else 'ConformantClosed')
    request('POST', f"/receptions/{reception['id']}/verify", verify, site, 409 if discrepant else 400)
    if discrepant:
        cases = request('GET', '/discrepancy-cases', token=site)
        case = next(c for c in cases if c['receptionId'] == str(reception['id']))
        request('PATCH', f"/discrepancy-cases/{case['id']}", {'status': 'Closed'}, site, 409)
        request('PATCH', f"/discrepancy-cases/{case['id']}", {'responsibility': 'Transport'}, site)
        request('PATCH', f"/discrepancy-cases/{case['id']}", {'status': 'Closed', 'resolutionSummary': 'Verified locally'}, site)
        assert len(request('GET', f"/discrepancy-cases/{case['id']}/timeline", token=site)) == 3
        report = request('GET', f"/discrepancy-cases/{case['id']}/report", token=site)
        assert b'%%EOF' in report and b'CASE TIMELINE' in report
        pathlib.Path(args.access_file).with_name('report-verification.pdf').write_bytes(report)
activity = request('GET', '/activity-records', token=admin)
assert any(a['userName'] == 'Local Site Resident' for a in activity)
assert any(a['userName'] == 'Local Warehouse Manager' for a in activity)
print(f'PASS: {checks} real HTTP checks; authentication, roles, persistence, order transitions, telemetry, receptions, cases and activity history.')
