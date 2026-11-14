namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

public class VehicleCommandService(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork) : IVehicleCommandService
{
    public async Task<Vehicle?> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken = default)
    {
        var licensePlate = new LicensePlate(command.LicensePlate);
        var existingByPlate = await vehicleRepository.FindByLicensePlateAsync(licensePlate.Value, cancellationToken);
        if (existingByPlate is not null)
            throw new FleetException(FleetErrorCodes.DuplicateLicensePlate,
                $"A vehicle with license plate '{licensePlate.Value}' is already registered.");

        string? normalizedGpsId = null;
        if (!string.IsNullOrWhiteSpace(command.GpsDeviceId))
        {
            var gpsDevice = new GpsDeviceId(command.GpsDeviceId);
            var existingByGps = await vehicleRepository.FindByGpsDeviceIdAsync(gpsDevice.Value, cancellationToken);
            if (existingByGps is not null)
                throw new FleetException(FleetErrorCodes.DeviceAlreadyAssigned,
                    $"GPS device '{gpsDevice.Value}' is already assigned to vehicle '{existingByGps.LicensePlate.Value}'.");
            normalizedGpsId = gpsDevice.Value;
        }

        var capacity = new VehicleCapacity(command.MaxCapacityTons);
        var vehicle = new Vehicle(
            command.CompanyId,
            licensePlate,
            command.Brand,
            command.Model,
            capacity,
            normalizedGpsId
        );

        await vehicleRepository.AddAsync(vehicle, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return vehicle;
    }

    public async Task<Vehicle?> Handle(PairGpsDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(command.VehicleId, cancellationToken)
            ?? throw new FleetException(FleetErrorCodes.VehicleNotFound,
                $"Vehicle with ID {command.VehicleId} was not found.");

        var gpsDevice = new GpsDeviceId(command.GpsDeviceId);
        var assignedVehicle = await vehicleRepository.FindByGpsDeviceIdAsync(gpsDevice.Value, cancellationToken);
        if (assignedVehicle is not null && assignedVehicle.Id != vehicle.Id)
            throw new FleetException(FleetErrorCodes.DeviceAlreadyAssigned,
                $"GPS device '{gpsDevice.Value}' is already assigned to vehicle '{assignedVehicle.LicensePlate.Value}'.");

        vehicle.PairGpsDevice(gpsDevice.Value);

        vehicleRepository.Update(vehicle);
        await unitOfWork.CompleteAsync(cancellationToken);
        return vehicle;
    }

    public async Task<Vehicle?> Handle(UpdateVehiclePositionCommand command, CancellationToken cancellationToken = default)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(command.VehicleId, cancellationToken)
            ?? throw new FleetException(FleetErrorCodes.VehicleNotFound,
                $"Vehicle with ID {command.VehicleId} was not found.");

        var location = new Location(command.Latitude, command.Longitude);
        vehicle.UpdatePosition(location);

        vehicleRepository.Update(vehicle);
        await unitOfWork.CompleteAsync(cancellationToken);
        return vehicle;
    }
}
