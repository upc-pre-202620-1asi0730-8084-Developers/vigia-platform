namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

/// <summary>
/// Type of property/site bounded by a geofence in Vigía.
/// Corresponds directly to frontend GeofenceType: WAREHOUSE / JOB_SITE.
/// </summary>
public enum GeofenceType
{
    Warehouse = 1,
    JobSite = 2
}
