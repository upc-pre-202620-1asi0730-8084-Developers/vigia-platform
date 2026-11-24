namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Entities;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public class GeofenceSiteRegistration
{
    public string CompanyId { get; private set; } = "";
    public string SiteId { get; private set; } = "";
    public int GeofenceId { get; private set; }
    public Geofence Geofence { get; private set; } = default!;

    private GeofenceSiteRegistration() { }
    public GeofenceSiteRegistration(Geofence geofence)
    {
        CompanyId = geofence.CompanyId;
        SiteId = geofence.SiteId;
        Geofence = geofence;
    }
}
