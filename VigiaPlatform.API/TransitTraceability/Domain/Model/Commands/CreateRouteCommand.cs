using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;

public record CreateRouteCommand(
    string DispatchId,
    int VehicleId,
    string VehiclePlate,
    string OriginName,
    int DestinationGeofenceId,
    string DestinationName,
    double DestinationLatitude,
    double DestinationLongitude,
    double DestinationRadiusMeters,
    IReadOnlyList<GeoPoint> PlannedPath,
    DateTimeOffset StartedAt,
    DateTimeOffset EstimatedArrival,
    double? DeviationThresholdMeters,
    int? StopThresholdMinutes);
