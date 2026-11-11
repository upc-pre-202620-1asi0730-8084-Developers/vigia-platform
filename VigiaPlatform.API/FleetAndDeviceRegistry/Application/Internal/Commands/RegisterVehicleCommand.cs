namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;

public record RegisterVehicleCommand(
    string CompanyId,
    string LicensePlate,
    string Brand,
    string Model,
    double MaxCapacityTons,
    string? GpsDeviceId = null
);
