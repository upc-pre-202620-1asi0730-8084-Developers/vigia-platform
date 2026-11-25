"""Run the release-candidate local integration checks without publishing a release."""
import argparse,json,pathlib,re,subprocess,sys,time,urllib.parse,urllib.request
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--wait-for-auth-window',action='store_true',help='Wait for an existing authentication rate-limit window before running suites.')
p.add_argument('--url',default='http://localhost:5179/api/v1');p.add_argument('--access-file',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/access.json'));p.add_argument('--output',default=str(pathlib.Path(__file__).resolve().parents[1]/'.local/candidate-results.json'));p.add_argument('--allow-local-writes',action='store_true',required=True);a=p.parse_args()
parsed=urllib.parse.urlparse(a.url)
if parsed.hostname not in ('localhost','127.0.0.1') or parsed.path!='/api/v1' or parsed.query or parsed.fragment:raise SystemExit('Use an isolated local API at /api/v1.')
origin=urllib.parse.urlunparse((parsed.scheme,parsed.netloc,'','','',''));checks=0
with urllib.request.urlopen(origin+'/health',timeout=30) as r:assert r.status==200;checks+=1
with urllib.request.urlopen(origin+'/openapi/v1.json',timeout=30) as r:document=json.load(r);checks+=1
scheme=document['components']['securitySchemes']['Bearer'];assert scheme['type']=='http' and scheme['scheme']=='bearer' and 'opaque' in scheme['bearerFormat'].lower()
for path,method in [('/api/v1/orders','get'),('/api/v1/discrepancy-cases/{id}/report','get'),('/api/v1/receptions/{receptionId}/evidence/files','post')]:
 assert {'Bearer':[]} in document['paths'][path][method]['security'],path;checks+=1
assert not document['paths']['/api/v1/authentication/sign-in']['post'].get('security');checks+=1
assert 'multipart/form-data' in document['paths']['/api/v1/receptions/{receptionId}/evidence/files']['post']['requestBody']['content'];checks+=1
with urllib.request.urlopen(origin+'/scalar/v1',timeout=30) as r:assert r.status==200 and b'scalar' in r.read().lower();checks+=1
if a.wait_for_auth_window:
 print('Waiting62s for the existing authentication window; protection stays enabled.',flush=True);time.sleep(62)
suites=[];root=pathlib.Path(__file__).resolve().parent
for index,name in enumerate(['verify-reception-integration.py','verify-evidence-files.py','verify-dispatch-transit.py','verify-local.py']):
 if index==2:
  print('Waiting62s between authentication-heavy suite groups; protection stays enabled.',flush=True);time.sleep(62)
 args=[sys.executable,str(root/name),'--url',a.url,'--access-file',a.access_file,'--allow-local-writes']
 if name in ('verify-reception-integration.py','verify-evidence-files.py'):args+=['--output',str(pathlib.Path(a.output).with_name(name.removesuffix('.py')+'-candidate.json'))]
 result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8',errors='replace')
 if result.returncode:print(result.stdout);print(result.stderr,file=sys.stderr);raise SystemExit(result.returncode)
 match=re.search(r'PASS: (\d+)',result.stdout);assert match,result.stdout
 suites.append({'script':name,'httpChecks':int(match[1])});print(result.stdout.strip(),flush=True)
report={'scope':'Local isolated database; no release, Azure or hardware publication','openApiScalarChecks':checks,'suites':suites,'httpChecks':checks+sum(x['httpChecks'] for x in suites),'scalarAuthentication':'Bearer declared only on authorized operations; anonymous sign-in remains public'}
pathlib.Path(a.output).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(f"PASS: candidate integration, {report['httpChecks']} HTTP checks including {checks} Scalar/OpenAPI checks.")
