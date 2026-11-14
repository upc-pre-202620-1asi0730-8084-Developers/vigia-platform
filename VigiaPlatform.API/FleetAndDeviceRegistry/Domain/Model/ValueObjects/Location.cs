namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;

/// <summary>
/// Value Object representing geographic coordinates (WGS84).
/// Validates latitude [-90, 90] and longitude [-180, 180], and computes geodesic distance using Haversine.
/// Compatible with frontend Location Value Object.
/// </summary>
public record Location
{
    private const double EarthRadiusMeters = 6371000.0;

    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public Location() { }

    public Location(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude < -90.0 || latitude > 90.0)
            throw new FleetException(FleetErrorCodes.InvalidCoordinates,
                $"Invalid latitude: {latitude}. Must be between -90 and 90 degrees.");

        if (double.IsNaN(longitude) || longitude < -180.0 || longitude > 180.0)
            throw new FleetException(FleetErrorCodes.InvalidCoordinates,
                $"Invalid longitude: {longitude}. Must be between -180 and 180 degrees.");

        Latitude = Math.Round(latitude, 6);
        Longitude = Math.Round(longitude, 6);
    }

    /// <summary>
    /// Computes the geodesic distance in meters to another location using the Haversine formula.
    /// </summary>
    public double GetDistanceTo(Location target)
    {
        ArgumentNullException.ThrowIfNull(target);

        static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        var dLat = ToRadians(target.Latitude - Latitude);
        var dLon = ToRadians(target.Longitude - Longitude);

        var lat1Rad = ToRadians(Latitude);
        var lat2Rad = ToRadians(target.Latitude);

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadiusMeters * c;
    }
}
