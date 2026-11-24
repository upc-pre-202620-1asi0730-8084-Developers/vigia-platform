import argparse,datetime as dt,json,pathlib,urllib.request,urllib.error,urllib.parse,uuid
from local_trip_fixture import fresh_vehicle,departure_plan
p=argparse.ArgumentParser(description='Verify stored photo bytes, authorization, atomic rejection, replay, limits and case linkage against an isolated local API.')
p.add_argument('--url',default='http://127.0.0.1:5177/api/v1');p.add_argument('--access-file',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/access.json'));p.add_argument('--output',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/evidence-results.json'));p.add_argument('--allow-local-writes',action='store_true',required=True);a=p.parse_args()
if urllib.parse.urlparse(a.url).hostname not in ('localhost','127.0.0.1'):raise SystemExit('Only an isolated local API is supported.')
password=json.loads(pathlib.Path(a.access_file).read_text(encoding='utf-8-sig'))['password'];checks=0;findings=[];observations={}
def request(method,path,body=None,token=None,status=200):
 global checks
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request(a.url+path,method=method,data=None if body is None else json.dumps(body).encode(),headers=headers)
 try:
  with urllib.request.urlopen(req,timeout=30) as r:code,data=r.status,r.read()
 except urllib.error.HTTPError as e:code,data=e.code,e.read()
 assert code in (status if isinstance(status,tuple) else (status,)),f'{method} {path}: expected {status}, got {code}: {data[:300]!r}'
 checks+=1
 try:value=data if data.startswith(b'%PDF-') else json.loads(data) if data else None
 except json.JSONDecodeError:value=data.decode('utf-8',errors='replace')
 return value if not isinstance(status,tuple) else (code,value)
def finding(id,owner,message,observed):
 findings.append({'id':id,'owner':owner,'status':'Open','message':message,'observed':observed})
tokens={r:request('POST','/authentication/sign-in',{'username':r+'.local','password':password})['accessToken'] for r in ('warehouse','site','admin')};w,s,admin=(tokens[r] for r in ('warehouse','site','admin'))
username='contract.outsider.'+uuid.uuid4().hex[:6];request('POST','/authentication/sign-up',{'username':username,'password':password});outsider=request('POST','/authentication/sign-in',{'username':username,'password':password})['accessToken']
assert request('GET','/orders/request-catalog',token=outsider)==[]
project=request('GET','/orders/request-catalog',token=s)[0];material=project['materials'][0]
order=request('POST','/orders/requests',{'projectId':project['id'],'priority':'AVERAGE','date':dt.datetime.now(dt.timezone.utc).date().isoformat(),'items':[{'materialId':material['id'],'quantity':3}]},s,201)
op=f"/orders/{order['id']}/status";request('PATCH',op,{'status':'UNDER_REVIEW','performedBy':'spoofed'},w);approved=request('PATCH',op,{'status':'APPROVED','performedBy':'spoofed'},w);assert approved['items'][0]['quantity']==3
plan={'projectName':project['name'],'relatedOrderId':str(order['id']),'departureDate':dt.datetime.now(dt.timezone.utc).date().isoformat(),'estimatedTime':'18:00','dispatchType':'DEPARTURE_TO_WORK','priority':'AVERAGE'}
def ready():
 v=fresh_vehicle(request,w);d=request('POST','/dispatches/from-order',plan,w,201);path=f"/dispatches/{d['id']}"
 assert d['siteId']==str(project['id']) and d['relatedOrderId']==str(order['id']) and d['items'][0]['quantity']==3
 request('PUT',path+'/vehicle',{'vehicleId':v['id'],'vehiclePlate':v['licensePlate'],'performedBy':'spoofed'},w)
 weight=request('POST',path+'/weighing',{'weightKg':100,'expectedWeightKg':100,'toleranceKg':0},w,201)
 guide=request('POST',path+'/delivery-guide',{'driverName':'Contract driver','carrierName':'Contract carrier'},w,201)
 assert guide['dispatchId']==d['id'] and guide['weighingId']==weight['id'] and guide['weightKg']==100
 departure=departure_plan(request,path,w);route=request('POST',path+'/departure',departure,w,201)
 assert route['dispatchId']==str(d['id']) and route['vehicleId']==v['id'] and route['gpsDeviceId']==v['gpsDeviceId']
 report=request('POST','/telemetry/reports',{'gpsDeviceId':v['gpsDeviceId'],**departure['plannedPath'][-1],'speedKmh':0,'timestamp':dt.datetime.now(dt.timezone.utc).isoformat()},w)
 assert report['tracked'] and report['routeId']==route['id'] and report['status']=='ARRIVED'
 assert request('GET',path,token=w)['status']=='IN_TRANSIT'
 arrival=next(x for x in request('GET','/upcoming-arrivals',token=s) if x['dispatchId']==str(d['id']))
 assert arrival['routeId']==route['id'] and arrival['deliveryGuideNumber']==guide['guideNumber'] and arrival['items'][0]['dispatchedQuantity']==3
 assert not any(x['dispatchId']==str(d['id']) for x in request('GET','/upcoming-arrivals',token=outsider))
 return d,path,guide,route
checklist={'materialGoodCondition':True,'quantityVerified':True,'deliveryGuideReceived':True,'photographicEvidence':False,'observationsRecorded':False}
def receipt_body(d,guide,quantity=3):return {'dispatchId':str(d['id']),'deliveryGuideNumber':guide['guideNumber'],'driverName':guide['driverName'],'items':[{'materialName':i['material'],'receivedQuantity':quantity} for i in d['items']],'checklist':checklist,'observations':''}

import base64,hashlib,struct,zlib,concurrent.futures

def png(width=16,height=16):
 def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
 raw=b''.join(b'\0'+bytes([30,120,180])*width for _ in range(height))
 return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',width,height,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b'')
image=png();pathlib.Path('.local/evidence-test.png').write_bytes(image)
def upload(rid,token=s,image=image,name='local-photo.png',mime='image/png',caption='Local receipt evidence',key=None,status=201):
 global checks
 boundary='Vigia'+uuid.uuid4().hex;key=key or str(uuid.uuid4())
 data=b''
 for field,value in [('description',caption),('requestId',key)]:data+=f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"\r\n\r\n{value}\r\n'.encode()
 data+=f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{name}"\r\nContent-Type: {mime}\r\n\r\n'.encode()+image+f'\r\n--{boundary}--\r\n'.encode()
 headers={'Content-Type':'multipart/form-data; boundary='+boundary}
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request(a.url+f'/receptions/{rid}/evidence/files',method='POST',data=data,headers=headers)
 try:
  with urllib.request.urlopen(req,timeout=30) as res:code,body=res.status,res.read()
 except urllib.error.HTTPError as e:code,body=e.code,e.read()
 assert code in (status if isinstance(status,tuple) else (status,)),f'upload expected {status}, got {code}: {body[:400]!r}'
 checks+=1
 return json.loads(body) if body else None

d,path,guide,route=ready();body=receipt_body(d,guide,2);body['observations']='One unit missing';body['checklist']={**checklist,'observationsRecorded':True}
r=request('POST','/receptions/physical',body,s,201);rid=r['id'];prefix=f'/receptions/{rid}'
for token,code in [(None,401),(w,403),(outsider,404)]:
 request('GET',prefix+'/evidence-context',token=token,status=code)
 upload(rid,token=token,status=code)
key=str(uuid.uuid4());photo=upload(rid,key=key);assert photo['receptionId']==rid and photo['dispatchId']==str(d['id'])
assert photo['sha256'].lower()==hashlib.sha256(image).hexdigest() and photo['sizeBytes']==len(image)
assert upload(rid,key=key)['id']==photo['id']
upload(rid,key=key,caption='Different payload',status=409)
for token,code in [(None,401),(w,403),(outsider,404)]:request('GET',photo['fileUrl'].removeprefix('/api/v1'),token=token,status=code)
with urllib.request.urlopen(urllib.request.Request(a.url.removesuffix('/api/v1')+photo['fileUrl'],headers={'Authorization':'Bearer '+s})) as res:
 assert res.read()==image and res.headers['Content-Type']=='image/png' and 'no-store' in res.headers['Cache-Control'] and res.headers['X-Content-Type-Options']=='nosniff';checks+=1
request('GET',f'/receptions/{rid+9999}/evidence/files/'+photo['id'],token=s,status=404)
for options in [dict(name='photo.html'),dict(image=b''),dict(image=b'<html>bad</html>'),dict(mime='image/jpeg'),dict(image=b'x'*(5*1024*1024+1)),dict(caption='x'*251),dict(caption='bad\ncaption'),dict(key='00000000-0000-0000-0000-000000000000'),dict(image=png(4001,4000))]:upload(rid,status=(400,413),**options)
request('POST',prefix+'/evidence',{'evidenceType':'PHOTO','fileUrl':photo['fileUrl'],'description':'Forged stored reference'},s,400)
assert len(request('GET',prefix+'/evidence-context',token=s)['evidences'])==1
parallel_key=str(uuid.uuid4())
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:results=list(pool.map(lambda _:upload(rid,key=parallel_key),range(2)))
assert results[0]['id']==results[1]['id'];assert len(request('GET',prefix+'/evidence-context',token=s)['evidences'])==2
for _ in range(18):upload(rid)
upload(rid,status=409)
assert len(request('GET',prefix+'/evidence-context',token=s)['evidences'])==20
request('POST',prefix+'/verify',{'status':'VERIFIED_DISCREPANT','checklist':body['checklist'],'observations':body['observations']},s)
case=next(c for c in request('GET','/discrepancy-cases',token=s) if c['receptionId']==str(rid));cid=case['id']
assert not any(c['id']==cid for c in request('GET','/discrepancy-cases',token=outsider))
for endpoint in ['', '/timeline','/report','/reception']:
 request('GET',f'/discrepancy-cases/{cid}'+endpoint,token=outsider,status=404)
 request('GET',f'/discrepancy-cases/{cid}'+endpoint,token=s)
request('PATCH',f'/discrepancy-cases/{cid}',{'responsibility':'Transport'},outsider,404)
request('POST','/discrepancy-cases',{'receptionId':str(rid),'dispatchId':str(d['id']),'caseNumber':'FORBIDDEN'},outsider,404)
request('POST','/discrepancy-cases',{'receptionId':str(rid),'dispatchId':'wrong','caseNumber':'MISMATCH'},s,400)
request('POST',prefix+'/evidence',{'evidenceType':'PHOTO','fileUrl':'https://example.test/reference.png','description':'Reference'},outsider,404)
request('PATCH',f'/discrepancy-cases/{cid}',{'status':'Closed'},s,409)
request('PATCH',f'/discrepancy-cases/{cid}',{'responsibility':'Transport'},s)
assert request('PATCH',f'/discrepancy-cases/{cid}',{'status':'Closed','resolutionSummary':'Carrier confirmed shortage'},s)['status']=='Closed'
# A conformant receipt locks new uploads; exact replay remains recoverable after closure.
d2,path2,guide2,route2=ready();r2=request('POST','/receptions/physical',receipt_body(d2,guide2),s,201);key2=str(uuid.uuid4());p2=upload(r2['id'],key=key2)
request('POST',f"/receptions/{r2['id']}/verify",{'status':'VERIFIED_CONFORMANT','checklist':checklist},s)
upload(r2['id'],status=409);assert upload(r2['id'],key=key2)['id']==p2['id']
report={'httpChecks':checks,'storedPhoto':photo,'caseId':cid,'receptionId':rid,'permissionIsolation':True,'exactReplay':True,'concurrentReplay':True,'invalidUploadAtomicity':True,'externalReceptionAccepted':False}
pathlib.Path(a.output).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(f'PASS: {checks} evidence HTTP checks; stored bytes, isolation, replay and case flow verified.')
