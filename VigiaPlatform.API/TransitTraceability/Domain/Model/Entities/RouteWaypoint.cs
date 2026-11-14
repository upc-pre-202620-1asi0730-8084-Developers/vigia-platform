using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

/// <summary>
/// Ordered waypoint of the planned path of a route.
/// </summary>
public class RouteWaypoint
{
    public int Id { get; private set; }
    public int RouteId { get; private set; }
    public int Sequence { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    private RouteWaypoint() { }

    internal RouteWaypoint(int sequence, double latitude, double longitude)
    {
        GeoValidation.EnsureCoordinates(latitude, longitude, TransitTraceabilityErrorCodes.InvalidRoute);
        Sequence = sequence;
        Latitude = latitude;
        Longitude = longitude;
    }

    public GeoPoint Point => new(Latitude, Longitude);
}
