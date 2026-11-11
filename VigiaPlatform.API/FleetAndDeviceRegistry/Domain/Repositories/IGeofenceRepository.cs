namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.Shared.Domain.Repositories;

/// <summary>
/// Domain repository interface for the Geofence Aggregate Root.
/// </summary>
public interface IGeofenceRepository : IBaseRepository<Geofence>
{
    Task<Geofence?> FindBySiteIdAsync(string siteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Geofence>> ListByTypeAsync(GeofenceType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Geofence>> ListByCompanyIdAsync(string companyId, CancellationToken cancellationToken = default);
}
