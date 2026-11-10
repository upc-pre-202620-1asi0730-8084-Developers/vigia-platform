using VigiaPlatform.API.TransitTraceability.Domain.Exceptions;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

/// <summary>
/// Snapshot of the destination site geofence of a route.
/// </summary>
public sealed class Destination
{
    public string Name { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double RadiusMeters { get; private set; }

    public Destination(string name, double latitude, double longitude, double radiusMeters)
    {
        GeoValidation.EnsureCoordinates(latitude, longitude, TransitTraceabilityErrorCodes.InvalidRoute);
        if (string.IsNullOrWhiteSpace(name))
            throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.InvalidRoute, "Destination name is required.");
        if (radiusMeters <= 0)
            throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.InvalidRoute, "Destination radius must be greater than zero.");

        Name = name.Trim();
        Latitude = latitude;
        Longitude = longitude;
        RadiusMeters = radiusMeters;
    }

    public GeoPoint Center => new(Latitude, Longitude);
}
