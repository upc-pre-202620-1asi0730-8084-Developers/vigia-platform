namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;

public record UpdateVehiclePositionCommand(
    int VehicleId,
    double Latitude,
    double Longitude
);
