import argparse,datetime as dt,json,pathlib,urllib.request,urllib.error,urllib.parse,uuid
from local_trip_fixture import fresh_vehicle,departure_plan
p=argparse.ArgumentParser(description='Review actual integration boundaries without claiming external reception acceptance.')
p.add_argument('--url',default='http://127.0.0.1:5171/api/v1');p.add_argument('--access-file',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/access.json'));p.add_argument('--output',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/integration-contract-results.json'));p.add_argument('--allow-local-writes',action='store_true',required=True);a=p.parse_args()
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
# Conforming receipt is exercised as the current external implementation, not accepted as Andre's delivery.
d,path,guide,route=ready();body=receipt_body(d,guide);reception=request('POST','/receptions/physical',body,s,201);rid=reception['id']
comparison=request('POST',f'/receptions/{rid}/compare',{'items':[{'id':i['id'],'receivedQuantity':i['receivedQuantity']} for i in reception['items']]},s);assert comparison['isConformant']
closed=request('POST',f'/receptions/{rid}/verify',{'status':'VERIFIED_CONFORMANT','checklist':checklist},s);assert closed['status']=='ConformantClosed'
state=request('GET',path,token=w)['status'];observations['conformantDispatchStatus']=state
if state!='COMPLETED':finding('F13-02','Andre A01; coordinate Fabian/Leonardo','Conforming receipt does not complete its dispatch; vehicle remains unavailable for another departure.',state)
code,foreign=request('GET',f'/receptions/{rid}',token=outsider,status=(200,403,404));observations['outsiderReceptionStatus']=code
if code==200:finding('F13-03','Andre A01/A02','Unassigned resident can read another project reception.',code)
# A discrepancy links case->reception->dispatch; PDF response is checked without claiming image upload.
d,path,guide,route=ready();body=receipt_body(d,guide,2);body['checklist']={**checklist,'observationsRecorded':True};body['observations']='One unit missing'
reception=request('POST','/receptions/physical',body,s,201);rid=reception['id']
request('POST',f'/receptions/{rid}/evidence',{'evidenceType':'PHOTO','fileUrl':'https://example.test/contract-evidence.jpg','description':'Reference only; no uploaded file'},s)
closed=request('POST',f'/receptions/{rid}/verify',{'status':'VERIFIED_DISCREPANT','checklist':body['checklist'],'observations':body['observations']},s);assert closed['status']=='DiscrepancyReported'
case=next(c for c in request('GET','/discrepancy-cases',token=s) if c['receptionId']==str(rid));assert case['dispatchId']==str(d['id'])
assert request('GET',f"/discrepancy-cases/{case['id']}/timeline",token=s)
request('PATCH',f"/discrepancy-cases/{case['id']}",{'status':'Closed'},s,409)
request('PATCH',f"/discrepancy-cases/{case['id']}",{'responsibility':'Transport'},s)
resolved=request('PATCH',f"/discrepancy-cases/{case['id']}",{'status':'Closed'},s)
assert resolved['status']=='Closed' and resolved['resolutionSummary'] is None
observations['caseClosure']='Liability required; resolution summary optional in current API'

pdf=request('GET',f"/discrepancy-cases/{case['id']}/report",token=s);assert pdf.startswith(b'%PDF-') and b'%%EOF' in pdf
code,_=request('GET',f"/discrepancy-cases/{case['id']}/report",token=outsider,status=(200,403,404));observations['outsiderCaseReportStatus']=code
if code==200:finding('F13-04','Elias row15/16; coordinate Andre A01','Unassigned resident can download another project discrepancy report.',code)
finding('F13-05','Elias row15/16; Andre A03','Evidence is only an external URL reference; persisted upload/download still requires verification.','No file-upload acceptance claimed')
# Probe guide integrity separately from project authorization so findings are attributable.
d,path,guide,route=ready();body=receipt_body(d,guide);body['deliveryGuideNumber']='GD-INVALID-CONTRACT'
code,_=request('POST','/receptions/physical',body,s,status=(201,400,409));observations['wrongGuideReceptionStatus']=code
if code==201:finding('F13-06','Andre A01','Reception accepts a guide number that does not match the dispatch immutable guide.',code)
d,path,guide,route=ready();code,_=request('POST','/receptions/physical',receipt_body(d,guide),outsider,status=(201,400,403,404,409));observations['outsiderRegistrationStatus']=code
if code==201:finding('F13-07','Andre A01','Unassigned resident can register a physical reception for another project.',code)
report={'review':'row13 integration contract review','httpChecks':checks,'foundationVerified':True,'externalReceptionAccepted':False,'andreDeliveryReceived':False,'observations':observations,'findings':findings,'scope':'Local isolated MySQL/API; no Azure, release or physical hardware acceptance'}
pathlib.Path(a.output).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'PASS: {checks} boundary checks; foundation verified. REVIEW: {len(findings)} external integration findings remain open; reception acceptance is pending.')
