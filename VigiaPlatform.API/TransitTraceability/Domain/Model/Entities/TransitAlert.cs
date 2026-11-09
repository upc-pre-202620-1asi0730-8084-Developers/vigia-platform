using VigiaPlatform.API.TransitTraceability.Domain.Exceptions;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

/// <summary>
/// Alert raised by a route while it is tracked (route deviation, prolonged stop,
/// signal lost/recovered, geofence entry).
/// </summary>
public class TransitAlert
{
    public int Id { get; private set; }
    public int RouteId { get; private set; }
    public TransitAlertType Type { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    /// <summary>Measured value: meters off route or minutes stopped.</summary>
    public double? Value { get; private set; }

    public DateTimeOffset? NotifiedAt { get; private set; }

    private TransitAlert() { }

    internal TransitAlert(TransitAlertType type, DateTimeOffset detectedAt, double? latitude, double? longitude, double? value)
    {
        Type = type;
        DetectedAt = detectedAt;
        Latitude = latitude;
        Longitude = longitude;
        Value = value;
    }

    /// <summary>Whether this alert type must be notified to the destination site.</summary>
    public bool RequiresNotification => Type.RequiresNotification();

    /// <summary>Whether the alert still has to be notified to the destination site.</summary>
    public bool IsPendingNotification => RequiresNotification && NotifiedAt is null;

    /// <summary>
    /// Marks the alert as notified to the destination site.
    /// </summary>
    internal void MarkAsNotified(DateTimeOffset notifiedAt)
    {
        if (!RequiresNotification)
            throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.AlertNotNotifiable,
                $"Alert type {Type} is not notifiable.");
        NotifiedAt = notifiedAt;
    }
}
