namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;

public record GeofenceResource(
    int Id,
    string CompanyId,
    string SiteId,
    string Name,
    string Type,
    LocationResource CenterPoint,
    double RadiusMeters,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt
);
