namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

/// <summary>
/// Lifecycle states of a route. A route starts InTransit when the vehicle leaves the warehouse geofence
/// and ends Arrived when it enters the destination site geofence.
/// </summary>
public enum RouteStatus
{
    InTransit,
    Deviated,
    Stopped,
    SignalLost,
    Arrived
}
