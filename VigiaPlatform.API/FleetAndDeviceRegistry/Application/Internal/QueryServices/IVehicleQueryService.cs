namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public interface IVehicleQueryService
{
    Task<IEnumerable<Vehicle>> Handle(GetAllVehiclesQuery query, CancellationToken cancellationToken = default);
    Task<Vehicle?> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken = default);
    Task<Vehicle?> Handle(GetVehicleByLicensePlateQuery query, CancellationToken cancellationToken = default);
}
