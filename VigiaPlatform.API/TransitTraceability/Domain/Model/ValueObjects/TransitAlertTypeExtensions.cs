namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

public static class TransitAlertTypeExtensions
{
    /// <summary>
    /// Alert types that must be notified to the destination site.
    /// </summary>
    public static bool RequiresNotification(this TransitAlertType type) =>
        type is TransitAlertType.RouteDeviation or TransitAlertType.ProlongedStop;
}
