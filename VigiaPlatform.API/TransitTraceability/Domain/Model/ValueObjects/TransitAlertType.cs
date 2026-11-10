namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

/// <summary>
/// Types of transit alerts raised while a route is being tracked.
/// </summary>
public enum TransitAlertType
{
    RouteDeviation,
    ProlongedStop,
    SignalLost,
    SignalRecovered,
    GeofenceEntry
}
