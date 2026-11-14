namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public interface IVehicleCommandService
{
    Task<Vehicle?> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken = default);
    Task<Vehicle?> Handle(PairGpsDeviceCommand command, CancellationToken cancellationToken = default);
    Task<Vehicle?> Handle(UpdateVehiclePositionCommand command, CancellationToken cancellationToken = default);
}
