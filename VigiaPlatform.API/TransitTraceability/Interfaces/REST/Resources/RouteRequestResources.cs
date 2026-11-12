namespace VigiaPlatform.API.TransitTraceability.Interfaces.REST.Resources;

public record CreateRouteResource(
    string DispatchId,
    int VehicleId,
    string VehiclePlate,
    string OriginName,
    int DestinationGeofenceId,
    DestinationResource Destination,
    IEnumerable<GeoPointResource> PlannedPath,
    DateTimeOffset StartedAt,
    DateTimeOffset EstimatedArrival,
    double? DeviationThresholdMeters,
    int? StopThresholdMinutes);

/// <summary>GPS position reported by the telemetry platform. ReportedAt defaults to the server time.</summary>
public record RegisterPositionResource(double Latitude, double Longitude, double SpeedKmh, DateTimeOffset? ReportedAt);

/// <summary>DetectedAt defaults to the server time.</summary>
public record ReportSignalLostResource(DateTimeOffset? DetectedAt);
