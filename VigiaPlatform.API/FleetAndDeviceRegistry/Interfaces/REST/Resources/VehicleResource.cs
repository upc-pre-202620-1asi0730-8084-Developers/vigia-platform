namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;

public record LocationResource(
    double Latitude,
    double Longitude
);

public record VehicleResource(
    int Id,
    string CompanyId,
    string LicensePlate,
    string Brand,
    string Model,
    double MaxCapacityTons,
    string? GpsDeviceId,
    bool IsGpsPaired,
    LocationResource? CurrentPos,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt
);
