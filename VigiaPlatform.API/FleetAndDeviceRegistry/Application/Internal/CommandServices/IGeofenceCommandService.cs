namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public interface IGeofenceCommandService
{
    Task<Geofence?> Handle(DefineGeofenceCommand command, CancellationToken cancellationToken = default);
}
