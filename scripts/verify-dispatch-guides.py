import argparse
from concurrent.futures import ThreadPoolExecutor
import datetime as dt
import json
import pathlib
import urllib.error
import urllib.parse
import urllib.request
import uuid

parser = argparse.ArgumentParser(description='Verify guide issuance and departure gates on an isolated local API.')
parser.add_argument('--url', default='http://127.0.0.1:5155/api/v1')
parser.add_argument('--access-file', default=str(pathlib.Path(__file__).resolve().parents[1] / '.local/access.json'))
parser.add_argument('--allow-local-writes', action='store_true', required=True)
args = parser.parse_args()
if urllib.parse.urlparse(args.url).hostname not in ('localhost', '127.0.0.1'):
    raise SystemExit('Only an isolated local API is supported.')
password = json.loads(pathlib.Path(args.access_file).read_text(encoding='utf-8-sig'))['password']
checks = 0


def request(method, path, body=None, token=None, expected=200, location=False):
    global checks
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer '+token
    req = urllib.request.Request(args.url+path, method=method, headers=headers,
                                 data=json.dumps(body).encode() if body is not None else None)
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            status, content, resource_location = response.status, response.read(), response.headers.get('Location')
    except urllib.error.HTTPError as error:
        status, content, resource_location = error.code, error.read(), None
    allowed = expected if isinstance(expected, tuple) else (expected,)
    assert status in allowed, f'{method} {path}: expected {allowed}, got {status}: {content[:300]!r}'
    checks += 1
    result = json.loads(content) if content else None
    return (result, resource_location) if location else result


tokens = {role: request('POST', '/authentication/sign-in', {'username': role+'.local', 'password': password})['accessToken']
          for role in ('site', 'warehouse', 'admin')}
site, warehouse, admin = (tokens[role] for role in ('site', 'warehouse', 'admin'))
approved = next(order for order in request('GET', '/orders', token=warehouse) if order['status'] == 'APPROVED')
vehicle = request('GET', '/vehicles', token=warehouse)[0]
plan = {'projectName': approved['projectName'], 'relatedOrderId': str(approved['id']),
        'departureDate': dt.datetime.now(dt.timezone.utc).date().isoformat(), 'estimatedTime': '18:00',
        'dispatchType': 'DEPARTURE_TO_WORK', 'priority': 'AVERAGE'}
dispatch = request('POST', '/dispatches/from-order', plan, warehouse, 201)
path = f"/dispatches/{dispatch['id']}"
guide_path = path+'/delivery-guide'
details = {'driverName': ' Local Driver ', 'carrierName': ' Local Carrier ', 'requestId': str(uuid.uuid4()),
           'guideNumber': 'SPOOFED', 'issuedBy': 'Spoofed actor', 'issuedAt': '2000-01-01T00:00:00Z', 'weightKg': 1}
request('POST', guide_path, details, expected=401)
request('POST', guide_path, details, site, 403)
request('GET', guide_path, token=site, expected=403)
request('GET', guide_path, token=warehouse, expected=404)
request('GET', '/dispatches/2147483647/delivery-guide', token=warehouse, expected=404)
request('POST', '/dispatches/2147483647/delivery-guide', details, warehouse, 404)
request('POST', guide_path, details, warehouse, 409)
request('PATCH', path+'/status', {'status': 'IN_TRANSIT', 'performedBy': 'Spoofed actor'}, warehouse, 409)
request('PUT', path+'/vehicle', {'vehicleId': vehicle['id'], 'vehiclePlate': vehicle['licensePlate'], 'performedBy': 'Current user'}, warehouse)
request('POST', guide_path, details, warehouse, 409)
request('PATCH', path+'/status', {'status': 'IN_TRANSIT', 'performedBy': 'Spoofed actor'}, warehouse, 409)
for measurement in ({'weightKg':100}, {'weightKg':103,'expectedWeightKg':100,'toleranceKg':2}):
    request('POST', path+'/weighing', measurement, warehouse, 201)
    request('POST', guide_path, details, warehouse, 409)
request('POST', path+'/weighing', {'weightKg':102,'expectedWeightKg':100,'toleranceKg':2}, warehouse, 201)
request('POST', path+'/items', {'code':'MAT-GUIDE','material':'Local additional material','quantity':2,'unit':'kg','performedBy':'Current user'}, warehouse)
request('POST', guide_path, details, warehouse, 409)
weigh_body = {'weightKg':100,'expectedWeightKg':100,'toleranceKg':0,'requestId':str(uuid.uuid4())}
weighing = request('POST', path+'/weighing', weigh_body, warehouse, 201)
for invalid in ({'driverName':''}, {'carrierName':None}, {'driverName':'x'*151}, {'carrierName':'Line\nBreak'}, {'requestId':str(uuid.UUID(int=0))}):
    request('POST', guide_path, {**details, **invalid}, warehouse, 400)
before = request('GET', path, token=warehouse)
events_before = len(before['timeline'])
activity_before = len(request('GET','/activity-records',token=admin))
guide, location = request('POST', guide_path, details, warehouse, 201, True)
assert location.endswith(guide_path)
assert guide['guideNumber'] != 'SPOOFED' and guide['dispatchId'] == dispatch['id']
assert guide['driverName'] == 'Local Driver' and guide['carrierName'] == 'Local Carrier'
assert guide['issuedBy'] == 'Local Warehouse Manager' and guide['issuedByAccountId'] > 0
assert dt.datetime.fromisoformat(guide['issuedAt'].replace('Z','+00:00')).year > 2000
assert guide['weightKg'] == 100 and guide['weighingId'] == weighing['id'] and guide['weightBasis'] == 'NET'
assert [(i['material'], i['quantity'], i['unit']) for i in guide['items']] == [(i['material'],i['quantity'],i['unit']) for i in before['items']]
current = request('GET',path,token=warehouse)
assert current['status'] == 'IN_PREPARATION' and current['isAuthorized'] and current['canDepart']
assert len(current['timeline']) == events_before+1
assert len(request('GET','/activity-records',token=admin)) == activity_before+1
stored = request('GET',guide_path,token=warehouse)
assert stored == guide, {key:(guide[key],stored[key]) for key in guide if guide[key] != stored[key]}
assert request('POST',guide_path,details,warehouse,201) == guide
request('POST',guide_path,{**details,'carrierName':'Different'},warehouse,409)
request('POST',guide_path,{**details,'requestId':str(uuid.uuid4())},warehouse,409)
assert len(request('GET',path,token=warehouse)['timeline']) == events_before+1
assert len(request('GET','/activity-records',token=admin)) == activity_before+1
request('POST',path+'/items',{'code':'NEW','material':'Not allowed','quantity':1,'unit':'kg','performedBy':'Current user'},warehouse,409)
request('DELETE',path+f"/items/{before['items'][0]['id']}",{'performedBy':'Current user'},warehouse,409)
request('PUT',path+'/vehicle',{'vehicleId':vehicle['id'],'vehiclePlate':vehicle['licensePlate'],'performedBy':'Current user'},warehouse,409)
request('POST',path+'/weighing',{**weigh_body,'requestId':str(uuid.uuid4())},warehouse,409)
assert request('POST',path+'/weighing',weigh_body,warehouse,201)['id'] == weighing['id']
request('PATCH',path+'/status',{'status':'IN_TRANSIT','performedBy':'Spoofed actor'},site,403)
departed = request('PATCH',path+'/status',{'status':'IN_TRANSIT','performedBy':'Spoofed actor'},warehouse)
assert departed['status'] == 'IN_TRANSIT' and not departed['canDepart'] and departed['isAuthorized']
arrival = next(item for item in request('GET','/upcoming-arrivals',token=site) if str(item['dispatchId']) == str(dispatch['id']))
assert arrival['deliveryGuideNumber'] == guide['guideNumber'] and arrival['driverName'] == 'Local Driver'
assert request('POST',guide_path,details,warehouse,201)['guideNumber'] == guide['guideNumber']
request('PATCH',path+'/status',{'status':'COMPLETED','performedBy':'Current user'},warehouse)
assert request('GET',guide_path,token=warehouse)['items'] == guide['items']

def prepared():
    other = request('POST','/dispatches/from-order',plan,warehouse,201)
    other_path = f"/dispatches/{other['id']}"
    request('PUT',other_path+'/vehicle',{'vehicleId':vehicle['id'],'vehiclePlate':vehicle['licensePlate'],'performedBy':'Current user'},warehouse)
    request('POST',other_path+'/weighing',weigh_body,warehouse,201)
    return other_path

other = prepared()
body = {**details,'requestId':str(uuid.uuid4())}
with ThreadPoolExecutor(max_workers=2) as pool:
    list(pool.map(lambda _: request('POST',other+'/delivery-guide',body,warehouse,(201,409)),range(2)))
replay = request('POST',other+'/delivery-guide',body,warehouse,201)
other_events = request('GET',other,token=warehouse)['timeline']
assert sum(e['event'] == 'Delivery guide issued' for e in other_events) == 1
request('POST',other+'/cancellation',{'reason':'Isolated local check','performedBy':'Current user'},warehouse)
cancelled = request('GET',other,token=warehouse)
assert not cancelled['isAuthorized'] and not cancelled['canDepart']
assert request('POST',other+'/delivery-guide',body,warehouse,201)['isAuthorized'] is False
request('PATCH',other+'/status',{'status':'IN_TRANSIT','performedBy':'Current user'},warehouse,409)
assert request('GET',other+'/delivery-guide',token=warehouse)['guideNumber'] == replay['guideNumber']
print(f'PASS: {checks} HTTP checks; guide prerequisites, inclusive/zero tolerance, stale load, roles, immutable snapshot, '
      'server identity/time, departure gate, replay, concurrent issuance, cancellation, activity and arrival contract.')
