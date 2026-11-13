namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;

public class VehicleQueryService(IVehicleRepository vehicleRepository) : IVehicleQueryService
{
    public async Task<IEnumerable<Vehicle>> Handle(GetAllVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.IsGpsPaired.HasValue)
            return await vehicleRepository.ListByGpsPairedStatusAsync(query.IsGpsPaired.Value, cancellationToken);

        return await vehicleRepository.ListAsync(cancellationToken);
    }

    public async Task<Vehicle?> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await vehicleRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<Vehicle?> Handle(GetVehicleByLicensePlateQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedPlate = LicensePlate.Normalize(query.LicensePlate);
        return await vehicleRepository.FindByLicensePlateAsync(normalizedPlate, cancellationToken);
    }
}
