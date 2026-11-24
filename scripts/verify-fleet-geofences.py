import argparse,json,pathlib,urllib.request,urllib.error,urllib.parse,uuid
from concurrent.futures import ThreadPoolExecutor
p=argparse.ArgumentParser(description='Verify fleet validation, GPS conflicts and geofence registration against an isolated local API.')
p.add_argument('--url',default='http://127.0.0.1:5167/api/v1');p.add_argument('--access-file',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/access.json'));p.add_argument('--allow-local-writes',action='store_true',required=True);a=p.parse_args();assert urllib.parse.urlparse(a.url).hostname in ('localhost','127.0.0.1')
password=json.loads(pathlib.Path(a.access_file).read_text(encoding='utf-8-sig'))['password'];checks=0

def call(method,path,body=None,token=None,status=200):
 global checks
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request(a.url+path,method=method,data=json.dumps(body).encode() if body is not None else None,headers=headers)
 try:
  with urllib.request.urlopen(req,timeout=30) as r:code,data=r.status,r.read()
 except urllib.error.HTTPError as e:code,data=e.code,e.read()
 assert code==status,f'{method} {path}: wanted {status}, got {code}: {data[:250]!r}'
 checks+=1;return json.loads(data) if data else None
w=call('POST','/authentication/sign-in',{'username':'warehouse.local','password':password})['accessToken'];s=call('POST','/authentication/sign-in',{'username':'site.local','password':password})['accessToken'];company=call('GET','/users/me',token=w)['companyId'];suffix=uuid.uuid4().hex[:6].upper();payload={'companyId':company,'licensePlate':'F11'+suffix,'brand':'Volvo','model':'Local','maxCapacityTons':8}
call('POST','/vehicles',payload,status=401);call('POST','/vehicles',payload,s,403)
v=call('POST','/vehicles',payload,w,201);assert v['gpsDeviceId'] is None
for field,value in [('licensePlate','!!!!!'),('licensePlate','A--12'),('brand','X'*101),('model','X'*101),('companyId','X'*65),('maxCapacityTons',0.001),('maxCapacityTons',0),('maxCapacityTons',151),('gpsDeviceId','G'*65),('gpsDeviceId','GPS\nBAD')]:call('POST','/vehicles',{**payload,'licensePlate':'X'+uuid.uuid4().hex[:6],field:value},w,400)
call('POST','/vehicles',{**payload,'licensePlate':'M11'+suffix,'maxCapacityTons':0.01},w,201)
call('POST','/vehicles',{**payload,'licensePlate':' '+payload['licensePlate'].lower()+' '},w,409)
b=call('POST','/vehicles',{**payload,'licensePlate':'B11'+suffix},w,201);gps='GPS-F11-'+suffix
for value in ['', 'X'*65,'BAD\rGPS']:call('POST',f"/vehicles/{v['id']}/gps-device",{'gpsDeviceId':value},w,400)
paired=call('POST',f"/vehicles/{v['id']}/gps-device",{'gpsDeviceId':' '+gps.lower()+' '},w);assert paired['gpsDeviceId']==gps
conflict=call('POST',f"/vehicles/{b['id']}/gps-device",{'gpsDeviceId':gps},w,409);assert conflict['code']=='FLEET_DEVICE_ALREADY_ASSIGNED' and conflict['assignedLicensePlate']==v['licensePlate']
call('POST','/vehicles',{**payload,'licensePlate':'C11'+suffix,'gpsDeviceId':gps},w,409)
call('POST','/telemetry/reports',{'gpsDeviceId':gps,'latitude':-12.07,'longitude':-77.03},w)
again=call('POST',f"/vehicles/{v['id']}/gps-device",{'gpsDeviceId':gps},w);assert again['currentPos']['latitude']==-12.07
replaced=call('POST',f"/vehicles/{v['id']}/gps-device",{'gpsDeviceId':gps+'-NEW'},w);assert replaced['currentPos'] is None
call('POST','/telemetry/reports',{'gpsDeviceId':gps+'-NEW','longitude':-77.03},w,400)
call('POST',f"/vehicles/{v['id']}/position",{'longitude':-77.03},w,400)
call('POST','/telemetry/reports',{'gpsDeviceId':gps+'-NEW','latitude':0,'longitude':0},w)
projects=call('GET','/projects',token=w);project=projects[0]
f={'companyId':company,'siteId':'W11-'+suffix,'name':'Local warehouse','type':'WAREHOUSE','centerPoint':{'latitude':-12.07,'longitude':-77.03},'radiusMeters':50000}
call('POST','/geofences',f,status=401);call('POST','/geofences',f,s,403)
g=call('POST','/geofences',f,w,201);assert g['radiusMeters']==50000
call('POST','/geofences',{**f,'siteId':' '+f['siteId'].lower()+' '},w,409)
for field,value in [('siteId','X'*65),('name','X'*151),('companyId','X'*65),('radiusMeters',9.99),('radiusMeters',50000.01),('centerPoint',{'longitude':0}),('centerPoint',{'latitude':0}),('centerPoint',{'latitude':91,'longitude':0}),('type','invalid')]:call('POST','/geofences',{**f,'siteId':'I11-'+uuid.uuid4().hex[:6],field:value},w,400)
call('POST','/geofences',{**f,'siteId':'missing-project-'+suffix,'type':'JOB_SITE'},w,400)
# Existing demo project already has a registered fence; use another company to exercise the same real project.
job=call('POST','/geofences',{**f,'companyId':'review-'+suffix,'siteId':str(project['id']),'type':'JOB_SITE','radiusMeters':10},w,201);assert job['siteId']==str(project['id'])
call('POST','/geofences',{**f,'companyId':'review-'+suffix,'siteId':'00'+str(project['id']),'type':'JOB_SITE'},w,409)
call('POST','/geofences',{**f,'siteId':'W11-OTHER-'+suffix,'radiusMeters':10},w,201)
call('GET',f"/geofences/{g['id']}",token=w)
call('GET','/vehicles?isGpsPaired=true',token=w)
# Both requests pass validation together; database-level registration must commit only one fence.
race={**f,'siteId':'RACE-'+suffix}
def concurrent(i):
 req=urllib.request.Request(a.url+'/geofences',method='POST',data=json.dumps(race).encode(),headers={'Content-Type':'application/json','Authorization':'Bearer '+w})
 try:
  with urllib.request.urlopen(req,timeout=30) as r:return r.status
 except urllib.error.HTTPError as e:return e.code
with ThreadPoolExecutor(max_workers=2) as pool:statuses=list(pool.map(concurrent,range(2)))
assert sorted(statuses)==[201,409],statuses
rows=call('GET','/geofences',token=w);assert sum(x['siteId']==race['siteId'] and x['companyId']==company for x in rows)==1
gps_race='GPS-RACE-'+suffix
race_vehicles=[call('POST','/vehicles',{**payload,'licensePlate':prefix+suffix},w,201) for prefix in ['R11','S11']]
def pair_race(vehicle):
 req=urllib.request.Request(a.url+f"/vehicles/{vehicle['id']}/gps-device",method='POST',data=json.dumps({'gpsDeviceId':gps_race}).encode(),headers={'Content-Type':'application/json','Authorization':'Bearer '+w})
 try:
  with urllib.request.urlopen(req,timeout=30) as r:return r.status,json.loads(r.read())
 except urllib.error.HTTPError as e:return e.code,json.loads(e.read())
with ThreadPoolExecutor(max_workers=2) as pool:gps_results=list(pool.map(pair_race,race_vehicles))
assert sorted(x[0] for x in gps_results)==[200,409],gps_results
winner=next(body for status,body in gps_results if status==200)
loser=next(body for status,body in gps_results if status==409)
assert loser['code']=='FLEET_DEVICE_ALREADY_ASSIGNED' and loser['assignedLicensePlate']==winner['licensePlate']
assert sum(v['gpsDeviceId']==gps_race for v in call('GET','/vehicles',token=w))==1
print(f'PASS: {checks} sequential HTTP checks + 4 concurrent requests; geofence201/409 and GPS200/409 with actual owner plate; fleet validation, reset, real project references and required coordinates.')
