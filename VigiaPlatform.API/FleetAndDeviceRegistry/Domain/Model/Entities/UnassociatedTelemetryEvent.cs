namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Entities;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

public class UnassociatedTelemetryEvent
{
    public int Id { get; private set; }
    public string GpsDeviceId { get; private set; } = "";
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    private UnassociatedTelemetryEvent() { }
    public UnassociatedTelemetryEvent(string gpsDeviceId, Location location, DateTimeOffset reportedAt)
    {
        if (string.IsNullOrWhiteSpace(gpsDeviceId) || gpsDeviceId.Length > 64)
            throw new ArgumentException("GPS device ID is required and must not exceed 64 characters.");
        GpsDeviceId = gpsDeviceId.Trim().ToUpperInvariant();
        Latitude = location.Latitude;
        Longitude = location.Longitude;
        ReportedAt = reportedAt;
        RegisteredAt = DateTimeOffset.UtcNow;
    }
}
