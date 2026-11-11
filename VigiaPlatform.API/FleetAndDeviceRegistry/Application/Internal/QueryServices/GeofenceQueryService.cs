namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;

public class GeofenceQueryService(IGeofenceRepository geofenceRepository) : IGeofenceQueryService
{
    public async Task<IEnumerable<Geofence>> Handle(GetAllGeofencesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Type.HasValue)
            return await geofenceRepository.ListByTypeAsync(query.Type.Value, cancellationToken);

        return await geofenceRepository.ListAsync(cancellationToken);
    }

    public async Task<Geofence?> Handle(GetGeofenceByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await geofenceRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<Geofence?> Handle(GetGeofenceBySiteIdQuery query, CancellationToken cancellationToken = default)
    {
        return await geofenceRepository.FindBySiteIdAsync(query.SiteId, cancellationToken);
    }
}
