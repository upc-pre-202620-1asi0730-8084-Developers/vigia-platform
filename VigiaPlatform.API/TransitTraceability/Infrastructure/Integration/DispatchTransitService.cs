namespace VigiaPlatform.API.TransitTraceability.Infrastructure.Integration;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;
using VigiaPlatform.API.TransitTraceability.Interfaces.REST.Resources;

public record DeparturePlan(int OriginGeofenceId, DateTimeOffset EstimatedArrival, GeoPointResource[] PlannedPath, Guid RequestId);
public class TripException(string code, string message, int status = 409) : Exception(message)
{ public string Code { get; } = code; public int Status { get; } = status; }

public class DispatchTransitService(AppDbContext db, IDispatchRepository dispatches, IUnitOfWork work)
{
    public static DateTimeOffset Precision(DateTimeOffset time) => new(time.UtcTicks / 10 * 10, TimeSpan.Zero);
    private IQueryable<TransitRoute> Routes() => db.Set<TransitRoute>().Include(r => r.Trip)
        .Include(r => r.Waypoints.OrderBy(p => p.Sequence)).Include(r => r.Positions.OrderBy(p => p.ReportedAt).ThenBy(p => p.Id))
        .Include(r => r.Alerts.OrderBy(a => a.DetectedAt).ThenBy(a => a.Id));

    public async Task<HashSet<string>> VisibleDispatchIdsAsync(string accountId, CancellationToken ct)
    {
        var sites = await db.Set<ProjectManagement.Domain.Model.ConstructionProject>().Where(p=>p.ManagerId==accountId).Select(p=>p.Id.ToString()).ToListAsync(ct);
        return (await db.Set<Dispatch>().Select(d=>new {d.Id,d.SiteId}).ToListAsync(ct)).Where(d=>sites.Contains(d.SiteId.Value)).Select(d=>d.Id.ToString()).ToHashSet();
    }
    public async Task<object> OptionsAsync(int id, string company, CancellationToken ct)
    {
        var dispatch = await dispatches.FindByIdAsync(id, ct) ?? throw new TripException("DISPATCH_NOT_FOUND", "Dispatch not found.",404);
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == dispatch.VehicleId.GetValueOrDefault().Value && v.CompanyId == company, ct);
        if(dispatch.VehicleId is not null && vehicle is null) throw new TripException("DISPATCH_NOT_FOUND","Dispatch not found.",404);
        var fences = await db.Geofences.AsNoTracking().Where(g => g.CompanyId == company).OrderBy(g => g.Id).ToListAsync(ct);
        var route = await Routes().SingleOrDefaultAsync(r => r.DispatchId == id.ToString(), ct);
        return new { dispatchId=id, gpsDeviceId=vehicle?.GpsDeviceId, canPlan=dispatch.CanDepart && vehicle?.IsGpsPaired == true,
            origins=fences.Where(g=>g.Type==GeofenceType.Warehouse).Select(Snapshot),
            destination=fences.FirstOrDefault(g=>g.Type==GeofenceType.JobSite && g.SiteId==dispatch.SiteId.Value) is {} destination ? Snapshot(destination) : null,
            routeId=route?.Id, requestId=route?.Trip?.RequestId, estimatedArrival=route?.EstimatedArrival };
    }
    private static object Snapshot(Geofence fence) => new {id=fence.Id,name=fence.Name,siteId=fence.SiteId,
        centerPoint=new {latitude=fence.CenterPoint.Latitude,longitude=fence.CenterPoint.Longitude},radiusMeters=fence.RadiusMeters};

    public async Task<TransitRoute> DepartAsync(int id, DeparturePlan input, string actor, string company, CancellationToken ct)
    {
        if(input.RequestId==Guid.Empty || input.OriginGeofenceId<=0 || input.PlannedPath is null || input.PlannedPath.Length is <2 or >500 || input.PlannedPath.Any(p=>p is null))
            throw new TripException("INVALID_DEPARTURE","Origin, request UUID and a path of 2 to 500 points are required.",400);
        var points=input.PlannedPath.Select(p=>new GeoPoint(p.Latitude,p.Longitude)).ToArray();
        var eta=Precision(input.EstimatedArrival);
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {input.OriginGeofenceId,eta,points}))));
        var existing=await Routes().SingleOrDefaultAsync(r=>r.DispatchId==id.ToString(),ct);
        if(existing is not null)
        {
            if(!await db.Vehicles.AnyAsync(v=>v.Id==existing.VehicleId && v.CompanyId==company,ct)) throw new TripException("DISPATCH_NOT_FOUND","Dispatch not found.",404);
            if(existing.Trip?.RequestId==input.RequestId && existing.Trip.RequestFingerprint==fingerprint) return existing;
            throw new TripException("DISPATCH_ALREADY_TRACKED","This dispatch already has a trip; refresh instead of starting it again.");
        }
        var dispatch=await dispatches.FindByIdAsync(id,ct) ?? throw new TripException("DISPATCH_NOT_FOUND","Dispatch not found.",404);
        if(!dispatch.CanDepart) throw new TripException("DEPARTURE_NOT_AUTHORIZED","A current conforming weighing and issued guide are required.");
        var vehicle=await db.Vehicles.SingleOrDefaultAsync(v=>v.Id==dispatch.VehicleId.GetValueOrDefault().Value && v.CompanyId==company,ct);
        if(vehicle is null || !vehicle.IsGpsPaired) throw new TripException("GPS_REQUIRED","The assigned vehicle must have a linked GPS in your company.");
        var assignedVehicleId = new VehicleId(vehicle.Id);
        if(await db.Set<DispatchTrip>().AnyAsync(t=>t.ActiveVehicleId==vehicle.Id,ct) ||
           await db.Set<Dispatch>().AnyAsync(d=>d.VehicleId==assignedVehicleId && d.Id!=id && d.Status==DispatchStatus.InTransit,ct))
            throw new TripException("VEHICLE_ALREADY_IN_TRANSIT","The vehicle already has an unfinished dispatch or trip.");
        var origin=await db.Geofences.SingleOrDefaultAsync(g=>g.Id==input.OriginGeofenceId && g.CompanyId==company && g.Type==GeofenceType.Warehouse,ct)
            ?? throw new TripException("ORIGIN_REQUIRED","Select an existing warehouse geofence in your company.");
        var destination=await db.Geofences.Where(g=>g.CompanyId==company && g.Type==GeofenceType.JobSite && g.SiteId==dispatch.SiteId.Value).OrderBy(g=>g.Id).FirstOrDefaultAsync(ct)
            ?? throw new TripException("DESTINATION_REQUIRED","The dispatch project needs a job-site geofence.");
        if(!origin.Contains(new Location(points[0].Latitude,points[0].Longitude)) ||
           !destination.Contains(new Location(points[^1].Latitude,points[^1].Longitude)))
            throw new TripException("INVALID_PLANNED_PATH","The path must start inside the warehouse and end inside the destination geofence.",400);
        var now=Precision(DateTimeOffset.UtcNow);
        if(eta<now) throw new TripException("INVALID_ESTIMATED_ARRIVAL","Estimated arrival must be after departure.",400);
        var route=new TransitRoute(id.ToString(),vehicle.Id,vehicle.LicensePlate.Value,origin.Name,destination.Id,
            new Destination(destination.Name,destination.CenterPoint.Latitude,destination.CenterPoint.Longitude,destination.RadiusMeters),points,now,eta);
        var trip=new DispatchTrip(id,route,vehicle.GpsDeviceId!,origin.Id,input.RequestId,fingerprint,actor);
        route.BindTrip(trip); vehicle.ReserveTracking();
        dispatch.ChangeStatus(DispatchStatus.InTransit,actor,"Trip started; available to the destination in upcoming arrivals.",now.UtcDateTime);
        db.Add(route);
        await work.CompleteAsync(ct);
        return route;
    }

    public async Task<object> ReportAsync(string deviceId, double latitude, double longitude, DateTimeOffset? timestamp, double? speedKmh, string company, CancellationToken ct)
    {
        var device=new GpsDeviceId(deviceId).Value;
        var position=new Location(latitude,longitude);
        var now=Precision(DateTimeOffset.UtcNow);var at=Precision(timestamp??now);
        var speed=speedKmh??0;
        if(at<DateTimeOffset.UnixEpoch || at>now.AddMinutes(5) || !double.IsFinite(speed) || speed<0 || speed>300)
            throw new TripException("INVALID_TELEMETRY","Check timestamp and speed; future timestamps beyond five minutes are not accepted.",400);
        var vehicle=await db.Vehicles.SingleOrDefaultAsync(v=>v.GpsDeviceId==device,ct);
        if(vehicle is null)
        {
            db.Add(new FleetAndDeviceRegistry.Domain.Model.Entities.UnassociatedTelemetryEvent(device,position,at));
            await work.CompleteAsync(ct);
            return new {accepted=false,recorded=true,reason="UNPAIRED_DEVICE"};
        }
        if(vehicle.CompanyId!=company) throw new TripException("FORBIDDEN_DEVICE","The GPS does not belong to your company.",403);
        if(vehicle.PositionReportedAt is {} last && at<last) throw new TripException("STALE_REPORT","This report is older than the stored position.");
        var route=await Routes().SingleOrDefaultAsync(r=>r.Trip!=null && r.Trip.ActiveVehicleId==vehicle.Id,ct);
        if(route is not null && at<route.StartedAt) throw new TripException("STALE_REPORT","The report predates departure.");
        if(vehicle.PositionReportedAt==at)
        {
            if(vehicle.CurrentPos!=position) throw new TripException("TIMESTAMP_CONFLICT","The timestamp was already used for a different position.");
            var replay=route??await Routes().Where(r=>r.Trip!=null && r.Trip.VehicleId==vehicle.Id && r.Trip.GpsDeviceId==device).OrderByDescending(r=>r.StartedAt).FirstOrDefaultAsync(ct);
            if(replay?.CurrentPosition?.ReportedAt!=at) replay=null;
            if(replay?.CurrentPosition is {} stored && stored.ReportedAt==at && stored.SpeedKmh!=speed)
                throw new TripException("TIMESTAMP_CONFLICT","The timestamp was already used for a different speed.");
            return new {accepted=true,replayed=true,tracked=replay is not null,vehicleId=vehicle.Id,routeId=replay?.Id,dispatchId=replay?.DispatchId};
        }
        if(route is null)
        {
            vehicle.UpdatePosition(position,at);await work.CompleteAsync(ct);
            return new {accepted=true,tracked=false,vehicleId=vehicle.Id,reason="NO_ACTIVE_TRIP"};
        }
        var dispatch=await dispatches.FindByIdAsync(route.Trip!.DispatchId,ct);
        if(dispatch?.Status!=DispatchStatus.InTransit || dispatch.VehicleId!=vehicle.Id || route.Trip.GpsDeviceId!=device)
            throw new TripException("TRIP_NOT_ACTIVE","The report does not match an active dispatch.");
        if(at<route.StartedAt) throw new TripException("STALE_REPORT","The report predates departure.");
        route.RegisterPosition(position.Latitude,position.Longitude,speed,at);
        vehicle.UpdatePosition(position,at);
        if(!route.IsActive) route.Trip.End("ARRIVED",at);
        await work.CompleteAsync(ct);
        return new {accepted=true,tracked=true,replayed=false,vehicleId=vehicle.Id,routeId=route.Id,dispatchId=route.DispatchId,
            status=Interfaces.REST.Transform.EnumContract.ToContract(route.Status)};
    }
}
