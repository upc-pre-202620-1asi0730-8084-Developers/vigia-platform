namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

public class GeofenceCommandService(IGeofenceRepository geofenceRepository, IUnitOfWork unitOfWork) : IGeofenceCommandService
{
    public async Task<Geofence?> Handle(DefineGeofenceCommand command, CancellationToken cancellationToken = default)
    {
        var centerPoint = new Location(command.Latitude, command.Longitude);
        var geofence = new Geofence(
            command.CompanyId,
            command.SiteId,
            command.Name,
            command.Type,
            centerPoint,
            command.RadiusMeters
        );

        await geofenceRepository.AddAsync(geofence, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return geofence;
    }
}
