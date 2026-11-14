using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

/// <summary>
/// GPS position reported by the telemetry platform for a route.
/// </summary>
public class TelemetryPosition
{
    public int Id { get; private set; }
    public int RouteId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double SpeedKmh { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; }

    private TelemetryPosition() { }

    internal TelemetryPosition(double latitude, double longitude, double speedKmh, DateTimeOffset reportedAt)
    {
        GeoValidation.EnsureCoordinates(latitude, longitude, TransitTraceabilityErrorCodes.InvalidPosition);
        Latitude = latitude;
        Longitude = longitude;
        SpeedKmh = double.IsNaN(speedKmh) || speedKmh < 0 ? 0 : speedKmh;
        ReportedAt = reportedAt;
    }

    public GeoPoint Point => new(Latitude, Longitude);

    /// <summary>
    /// Minutes elapsed between this position and a later one.
    /// </summary>
    public double MinutesUntil(TelemetryPosition later) => (later.ReportedAt - ReportedAt).TotalMinutes;
}
