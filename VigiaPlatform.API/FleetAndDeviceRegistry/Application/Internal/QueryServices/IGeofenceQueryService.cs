namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public interface IGeofenceQueryService
{
    Task<IEnumerable<Geofence>> Handle(GetAllGeofencesQuery query, CancellationToken cancellationToken = default);
    Task<Geofence?> Handle(GetGeofenceByIdQuery query, CancellationToken cancellationToken = default);
    Task<Geofence?> Handle(GetGeofenceBySiteIdQuery query, CancellationToken cancellationToken = default);
}
