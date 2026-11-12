namespace VigiaPlatform.API.TransitTraceability.Interfaces.REST.Resources;

public record GeoPointResource(double Latitude, double Longitude);

public record DestinationResource(string Name, double Latitude, double Longitude, double RadiusMeters);

public record TelemetryPositionResource(double Latitude, double Longitude, double SpeedKmh, DateTimeOffset ReportedAt);

public record TransitAlertResource(
    int Id,
    string Type,
    DateTimeOffset DetectedAt,
    double? Latitude,
    double? Longitude,
    double? Value,
    DateTimeOffset? NotifiedAt);

public record RouteResource(
    int Id,
    string DispatchId,
    int VehicleId,
    string VehiclePlate,
    string OriginName,
    int DestinationGeofenceId,
    DestinationResource Destination,
    IEnumerable<GeoPointResource> PlannedPath,
    IEnumerable<TelemetryPositionResource> Positions,
    IEnumerable<TransitAlertResource> Alerts,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset EstimatedArrival,
    DateTimeOffset? ArrivedAt,
    double DeviationThresholdMeters,
    int StopThresholdMinutes);

/// <summary>Route after a tracking command, with the alerts that command raised.</summary>
public record TransitOutcomeResource(RouteResource Route, IEnumerable<TransitAlertResource> RaisedAlerts);

public record ErrorResource(string Code, string Message);
