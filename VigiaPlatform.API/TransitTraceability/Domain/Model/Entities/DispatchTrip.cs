namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;

public class DispatchTrip
{
    public int DispatchId { get; private set; }
    public int RouteId { get; private set; }
    public TransitRoute Route { get; private set; } = default!;
    public int VehicleId { get; private set; }
    public int? ActiveVehicleId { get; private set; }
    public string GpsDeviceId { get; private set; } = "";
    public int OriginGeofenceId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = "";
    public DateTimeOffset DepartedAt { get; private set; }
    public string StartedBy { get; private set; } = "";
    public DateTimeOffset? EndedAt { get; private set; }
    public string? EndReason { get; private set; }
    private DispatchTrip() { }
    public DispatchTrip(int dispatchId, TransitRoute route, string device, int originId, Guid requestId, string fingerprint, string actor)
    {
        DispatchId = dispatchId; Route = route; VehicleId = route.VehicleId; ActiveVehicleId = route.VehicleId;
        GpsDeviceId = device; OriginGeofenceId = originId; RequestId = requestId; RequestFingerprint = fingerprint;
        DepartedAt = route.StartedAt; StartedBy = actor;
    }
    public void End(string reason, DateTimeOffset at) { ActiveVehicleId = null; EndReason = reason; EndedAt ??= at; }
}
