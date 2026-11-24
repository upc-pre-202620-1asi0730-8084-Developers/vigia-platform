import argparse,datetime as dt,json,pathlib,urllib.request,urllib.error,urllib.parse,uuid
from concurrent.futures import ThreadPoolExecutor
from local_trip_fixture import fresh_vehicle,departure_plan
p=argparse.ArgumentParser(description='Verify atomic dispatch departure and ordered GPS tracking on an isolated local API.')
p.add_argument('--url',default='http://127.0.0.1:5169/api/v1');p.add_argument('--access-file',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/access.json'));p.add_argument('--allow-local-writes',action='store_true',required=True);a=p.parse_args()
assert urllib.parse.urlparse(a.url).hostname in ('localhost','127.0.0.1')
password=json.loads(pathlib.Path(a.access_file).read_text(encoding='utf-8-sig'))['password'];checks=0

def request(method,path,body=None,token=None,status=200):
 global checks
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request(a.url+path,method=method,data=None if body is None else json.dumps(body).encode(),headers=headers)
 try:
  with urllib.request.urlopen(req,timeout=30) as r:code,data=r.status,r.read()
 except urllib.error.HTTPError as e:code,data=e.code,e.read()
 assert code in (status if isinstance(status,tuple) else (status,)),f'{method} {path}: expected {status}, got {code}: {data[:500]!r}'
 checks+=1;return json.loads(data) if data else None
w=request('POST','/authentication/sign-in',{'username':'warehouse.local','password':password})['accessToken']
s=request('POST','/authentication/sign-in',{'username':'site.local','password':password})['accessToken']
admin=request('POST','/authentication/sign-in',{'username':'admin.local','password':password})['accessToken']
approved=next(o for o in request('GET','/orders',token=w) if o['status']=='APPROVED')
vehicle=fresh_vehicle(request,w)
plan={'projectName':approved['projectName'],'relatedOrderId':str(approved['id']),'departureDate':dt.datetime.now(dt.timezone.utc).date().isoformat(),'estimatedTime':'18:00','dispatchType':'DEPARTURE_TO_WORK','priority':'AVERAGE'}
def prepared(v=vehicle,guide=True):
 d=request('POST','/dispatches/from-order',plan,w,201);path=f"/dispatches/{d['id']}"
 request('PUT',path+'/vehicle',{'vehicleId':v['id'],'vehiclePlate':v['licensePlate'],'performedBy':'Spoofed'},w)
 request('POST',path+'/weighing',{'weightKg':100,'expectedWeightKg':100,'toleranceKg':2},w,201)
 if guide:request('POST',path+'/delivery-guide',{'driverName':'Test driver','carrierName':'Test carrier'},w,201)
 return path
path=prepared(guide=False);body=departure_plan(request,path,w)
request('POST',path+'/departure',body,status=401);request('POST',path+'/departure',body,s,403);request('GET',path+'/departure',token=s,status=403)
request('POST',path+'/departure',body,w,409)
request('POST',path+'/delivery-guide',{'driverName':'Test driver','carrierName':'Test carrier'},w,201)
request('PATCH',path+'/status',{'status':'IN_TRANSIT','performedBy':'Spoofed'},w,409)
for change,status in [({'requestId':str(uuid.UUID(int=0))},400),({'originGeofenceId':0},400),({'plannedPath':[]},400),({'plannedPath':[None,None]},400),({'plannedPath':[{'longitude':0},{'latitude':0,'longitude':0}]},400),({'plannedPath':[{'latitude':91,'longitude':0},{'latitude':0,'longitude':0}]},400),({'estimatedArrival':'2000-01-01T00:00:00Z'},400),({'originGeofenceId':2147483647},409),({'plannedPath':[{'latitude':0,'longitude':0}]*2},400)]:
 request('POST',path+'/departure',{**body,**change},w,status)
route=request('POST',path+'/departure',{**body,'startedBy':'Spoofed','startedAt':'2000-01-01T00:00:00Z','vehicleId':2147483647},w,201)
rp=f"/routes/{route['id']}";assert route['trackingSource']=='DEVICE' and route['trackingActive'] and route['gpsDeviceId']==vehicle['gpsDeviceId'];assert route['vehicleId']==vehicle['id'];assert route['departureRequestId']==body['requestId']
assert request('POST',path+'/departure',body,w,201)['id']==route['id']
request('POST',path+'/departure',{**body,'requestId':str(uuid.uuid4())},w,409)
current=request('GET',path,token=w);assert current['status']=='IN_TRANSIT' and not current['canDepart'];assert current['timeline'][-1]['user']=='Local Warehouse Manager'
arrival=next(x for x in request('GET','/upcoming-arrivals',token=s) if x['dispatchId']==str(current['id']));assert arrival['routeId']==route['id'] and dt.datetime.fromisoformat(arrival['departedAt'].replace('Z','+00:00'))==dt.datetime.fromisoformat(route['startedAt'].replace('Z','+00:00')) and arrival['origin']==route['originName']
assert request('GET',rp,token=s)['id']==route['id']
username='other.site.'+uuid.uuid4().hex[:6];request('POST','/authentication/sign-up',{'username':username,'password':password});outsider=request('POST','/authentication/sign-in',{'username':username,'password':password})['accessToken'];assert request('GET','/upcoming-arrivals',token=outsider)==[];assert request('GET','/routes',token=outsider)==[];request('GET',rp,token=outsider,status=404);request('GET',rp+'/alerts',token=outsider,status=404)
request('POST',rp+'/positions',{'latitude':0,'longitude':0},w,409);request('POST',rp+'/positions',{'latitude':0,'longitude':0},s,403)
request('POST',f"/vehicles/{vehicle['id']}/gps-device",{'gpsDeviceId':vehicle['gpsDeviceId']+'-OTHER'},w,409)
assert request('POST',f"/vehicles/{vehicle['id']}/gps-device",{'gpsDeviceId':vehicle['gpsDeviceId']},w)['gpsDeviceId']==vehicle['gpsDeviceId']
request('POST',f"/vehicles/{vehicle['id']}/position",{'latitude':0,'longitude':0},w,409)
other=prepared();request('POST',other+'/departure',departure_plan(request,other,w),w,409)
started=dt.datetime.fromisoformat(route['startedAt'].replace('Z','+00:00'));stamp=dt.datetime.now(dt.timezone.utc)+dt.timedelta(seconds=1)
report={'gpsDeviceId':vehicle['gpsDeviceId'],**body['plannedPath'][0],'speedKmh':30,'timestamp':stamp.isoformat()}
result=request('POST','/telemetry/reports',report,w);assert result['tracked'] and result['routeId']==route['id'] and not result['replayed']
assert request('POST','/telemetry/reports',report,w)['replayed']
request('POST','/telemetry/reports',{**report,'speedKmh':31},w,409)
request('POST','/telemetry/reports',{**report,'timestamp':(started-dt.timedelta(seconds=1)).isoformat()},w,409)
request('POST','/telemetry/reports',{**report,'timestamp':(stamp+dt.timedelta(minutes=6)).isoformat()},w,400)
request('POST','/telemetry/reports',{**report,'speedKmh':301},w,400)
tracked=request('GET',rp,token=w);assert len(tracked['positions'])==1
assert request('GET',f"/vehicles/{vehicle['id']}",token=w)['currentPos']==body['plannedPath'][0]
# A new report creates a genuine deviation alert in the active trip.
deviation={**report,'latitude':report['latitude']+.02,'timestamp':(stamp+dt.timedelta(seconds=1)).isoformat()}
request('POST','/telemetry/reports',deviation,w);tracked=request('GET',rp,token=w);assert len(tracked['positions'])==2 and any(x['type']=='ROUTE_DEVIATION' for x in tracked['alerts'])
arrival_report={**report,**body['plannedPath'][-1],'timestamp':(stamp+dt.timedelta(seconds=2)).isoformat()};request('POST','/telemetry/reports',arrival_report,w)
arrived=request('GET',rp,token=w);assert arrived['status']=='ARRIVED' and not arrived['trackingActive'];assert request('GET',path,token=w)['status']=='IN_TRANSIT'
assert request('POST','/telemetry/reports',arrival_report,w)['replayed'];assert len(request('GET',rp,token=w)['positions'])==3
request('POST',other+'/departure',departure_plan(request,other,w),w,409)
request('PATCH',path+'/status',{'status':'COMPLETED','performedBy':'Spoofed'},w)
second=departure_plan(request,other,w);second_route=request('POST',other+'/departure',second,w,201)
request('POST',other+'/cancellation',{'reason':'Local check','performedBy':'Spoofed'},w,409)
request('PATCH',other+'/status',{'status':'COMPLETED','performedBy':'Spoofed'},w)
assert not request('GET',f"/routes/{second_route['id']}",token=w)['trackingActive']
no_trip=request('POST','/telemetry/reports',{**report,'timestamp':(stamp+dt.timedelta(seconds=3)).isoformat()},w);assert not no_trip['tracked'] and no_trip['reason']=='NO_ACTIVE_TRIP'
assert request('POST',path+'/departure',body,w,201)['id']==route['id']
unpaired=request('POST','/telemetry/reports',{**report,'gpsDeviceId':'UNPAIRED-T12-'+uuid.uuid4().hex[:8]},w);assert unpaired=={'accepted':False,'recorded':True,'reason':'UNPAIRED_DEVICE'}
# Two ready dispatches compete for one vehicle. Exactly one route must commit.
race_vehicle=fresh_vehicle(request,w);paths=[prepared(race_vehicle) for _ in range(2)];plans=[departure_plan(request,p,w) for p in paths]
def race(i):return request('POST',paths[i]+'/departure',plans[i],w,(201,409))
with ThreadPoolExecutor(max_workers=2) as pool: results=list(pool.map(race,range(2)))
assert sum('trackingSource' in x for x in results)==1,results
print(f'PASS: {checks} HTTP checks, including concurrent departures, role/site isolation, atomic departure prerequisites, replay, GPS lock, chronological telemetry, deviation, arrival and cancellation.')
