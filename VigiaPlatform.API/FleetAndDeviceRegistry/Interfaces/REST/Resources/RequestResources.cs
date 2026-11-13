namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;

public record RegisterVehicleResource(
    string CompanyId,
    string LicensePlate,
    string Brand,
    string Model,
    double MaxCapacityTons,
    string? GpsDeviceId = null
);

public record PairGpsDeviceResource(
    string GpsDeviceId
);

public record UpdateVehiclePositionResource(
    double Latitude,
    double Longitude
);

public record DefineGeofenceResource(
    string CompanyId,
    string SiteId,
    string Name,
    string Type,
    LocationResource? CenterPoint = null,
    double? Latitude = null,
    double? Longitude = null,
    double RadiusMeters = 100.0
);
