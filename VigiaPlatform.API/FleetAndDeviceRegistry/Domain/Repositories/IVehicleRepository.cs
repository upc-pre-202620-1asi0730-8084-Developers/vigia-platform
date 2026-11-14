namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Domain.Repositories;

/// <summary>
/// Domain repository interface for the Vehicle Aggregate Root.
/// </summary>
public interface IVehicleRepository : IBaseRepository<Vehicle>
{
    Task<Vehicle?> FindByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);
    Task<Vehicle?> FindByGpsDeviceIdAsync(string gpsDeviceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> ListByGpsPairedStatusAsync(bool isGpsPaired, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> ListByCompanyIdAsync(string companyId, CancellationToken cancellationToken = default);
}
