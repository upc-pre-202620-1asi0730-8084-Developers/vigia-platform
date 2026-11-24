namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;

public record LocationResource(
    [property: System.Text.Json.Serialization.JsonRequired] double Latitude,
    [property: System.Text.Json.Serialization.JsonRequired] double Longitude
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
