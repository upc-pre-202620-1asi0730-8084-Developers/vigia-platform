namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;

public record PairGpsDeviceCommand(
    int VehicleId,
    string GpsDeviceId
);
