namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

/// <summary>
/// Business error returned by the command services of the Transit Traceability bounded context.
/// </summary>
/// <param name="Code">Stable machine-readable code (see <see cref="TransitTraceabilityErrorCodes"/>).</param>
/// <param name="Message">Human-readable description.</param>
public sealed record TransitTraceabilityError(string Code, string Message);

public static class TransitTraceabilityErrorCodes
{
    public const string RouteNotFound = "ROUTE_NOT_FOUND";
    public const string AlertNotFound = "ALERT_NOT_FOUND";
    public const string RouteAlreadyArrived = "ROUTE_ALREADY_ARRIVED";
    public const string SignalAlreadyLost = "SIGNAL_ALREADY_LOST";
    public const string DispatchAlreadyTracked = "DISPATCH_ALREADY_TRACKED";
    public const string AlertNotNotifiable = "ALERT_NOT_NOTIFIABLE";
    public const string InvalidPosition = "INVALID_POSITION";
    public const string InvalidRoute = "INVALID_ROUTE";
}
