namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Transform;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;

public static class VehicleAssembler
{
    public static RegisterVehicleCommand ToCommandFromResource(RegisterVehicleResource resource)
    {
        return new RegisterVehicleCommand(
            resource.CompanyId,
            resource.LicensePlate,
            resource.Brand,
            resource.Model,
            resource.MaxCapacityTons,
            resource.GpsDeviceId
        );
    }

    public static PairGpsDeviceCommand ToCommandFromResource(int vehicleId, PairGpsDeviceResource resource)
    {
        return new PairGpsDeviceCommand(vehicleId, resource.GpsDeviceId);
    }

    public static UpdateVehiclePositionCommand ToCommandFromResource(int vehicleId, UpdateVehiclePositionResource resource)
    {
        return new UpdateVehiclePositionCommand(vehicleId, resource.Latitude, resource.Longitude);
    }

    public static VehicleResource ToResourceFromEntity(Vehicle entity)
    {
        var currentPos = entity.CurrentPos is not null
            ? new LocationResource(entity.CurrentPos.Latitude, entity.CurrentPos.Longitude)
            : null;

        return new VehicleResource(
            entity.Id,
            entity.CompanyId,
            entity.LicensePlate.Value,
            entity.Brand,
            entity.Model,
            entity.MaxCapacityTons.Tons,
            entity.GpsDeviceId,
            entity.IsGpsPaired,
            currentPos,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
