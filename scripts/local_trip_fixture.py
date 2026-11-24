import datetime as dt, uuid

def fresh_vehicle(request, token):
    suffix=uuid.uuid4().hex[:7].upper()
    company=request('GET','/users/me',token=token)['companyId']
    return request('POST','/vehicles',{'companyId':company,'licensePlate':'T12'+suffix,'brand':'Local','model':'Trip integration','maxCapacityTons':30,'gpsDeviceId':'GPS-T12-'+suffix},token,201)

def departure_plan(request, path, token):
    options=request('GET',path+'/departure',token=token)
    origin=options['origins'][0]
    return {'originGeofenceId':origin['id'],'estimatedArrival':(dt.datetime.now(dt.timezone.utc)+dt.timedelta(hours=2)).isoformat(),'plannedPath':[origin['centerPoint'],options['destination']['centerPoint']],'requestId':str(uuid.uuid4())}

def depart(request, path, token):
    request('POST',path+'/departure',departure_plan(request,path,token),token,201)
    return request('GET',path,token=token)
