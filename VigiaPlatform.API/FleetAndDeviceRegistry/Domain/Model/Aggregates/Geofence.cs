namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

using VigiaPlatform.API.Shared.Domain.Model;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

/// <summary>
/// Geofence Aggregate Root for the Fleet and Device Registry Bounded Context (BC-04).
/// Models perimeter boundaries for Warehouses and Construction Job Sites.
/// Supports contains-evaluation for entry/exit event triggering in Transit Traceability.
/// </summary>
public class Geofence : IAuditableEntity
{
    public int Id { get; private set; }
    public string CompanyId { get; private set; } = string.Empty;
    public string SiteId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public GeofenceType Type { get; private set; }
    public Location CenterPoint { get; private set; } = default!;
    public double RadiusMeters { get; private set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Geofence() { }

    /// <summary>
    /// Executes "Define the warehouse geofence" or "Define the construction site geofence" command.
    /// </summary>
    public Geofence(string companyId, string siteId, string name, GeofenceType type, Location centerPoint, double radiusMeters = 100.0)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            throw new FleetException(FleetErrorCodes.InvalidGeofence, "Company identifier is required.");
        if (string.IsNullOrWhiteSpace(siteId))
            throw new FleetException(FleetErrorCodes.InvalidGeofence, "Site identifier (warehouse or job site) is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new FleetException(FleetErrorCodes.InvalidGeofence, "Geofence name is required.");
        if (radiusMeters < 10.0 || radiusMeters > 50000.0)
            throw new FleetException(FleetErrorCodes.InvalidRadius,
                $"Geofence radius must be between 10 and 50,000 meters. Received: {radiusMeters}");

        CompanyId = companyId.Trim();
        SiteId = siteId.Trim();
        Name = name.Trim();
        Type = type;
        CenterPoint = centerPoint ?? throw new FleetException(FleetErrorCodes.InvalidCoordinates, "Center point is required.");
        RadiusMeters = Math.Round(radiusMeters, 2);
    }

    /// <summary>
    /// Evaluates if a given coordinate is located inside this geofence perimeter.
    /// </summary>
    public bool Contains(Location location)
    {
        if (location is null || CenterPoint is null) return false;
        var distance = CenterPoint.GetDistanceTo(location);
        return distance <= RadiusMeters;
    }

    public void UpdateBoundary(Location centerPoint, double radiusMeters)
    {
        if (radiusMeters < 10.0 || radiusMeters > 50000.0)
            throw new FleetException(FleetErrorCodes.InvalidRadius, "Invalid geofence radius.");

        CenterPoint = centerPoint ?? throw new FleetException(FleetErrorCodes.InvalidCoordinates, "Center point is required.");
        RadiusMeters = Math.Round(radiusMeters, 2);
    }
}
