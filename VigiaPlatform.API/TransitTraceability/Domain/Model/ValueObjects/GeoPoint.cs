namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

/// <summary>
/// Geographic coordinate in decimal degrees with the distance calculations used to track a route.
/// </summary>
public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    private const double EarthRadiusMeters = 6371000;

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Haversine distance to another coordinate.
    /// </summary>
    /// <returns>Distance in meters.</returns>
    public double DistanceTo(GeoPoint other)
    {
        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Shortest distance to a path segment, using a local equirectangular projection
    /// (accurate enough for the few kilometers between two route waypoints).
    /// </summary>
    /// <returns>Distance in meters.</returns>
    public double DistanceToSegment(GeoPoint start, GeoPoint end)
    {
        var metersPerDegreeLat = EarthRadiusMeters * Math.PI / 180;
        var metersPerDegreeLon = metersPerDegreeLat * Math.Cos(ToRadians(Latitude));
        var origin = this;
        (double X, double Y) Project(GeoPoint point) =>
            ((point.Longitude - origin.Longitude) * metersPerDegreeLon, (point.Latitude - origin.Latitude) * metersPerDegreeLat);

        var a = Project(start);
        var b = Project(end);
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1, -(a.X * dx + a.Y * dy) / lengthSquared));
        return Math.Sqrt(Math.Pow(a.X + t * dx, 2) + Math.Pow(a.Y + t * dy, 2));
    }
}
