namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

public class VehicleCommandService(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IVehicleTripGuard tripGuard) : IVehicleCommandService
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
                    $"GPS device '{gpsDevice.Value}' is already assigned to vehicle '{existingByGps.LicensePlate.Value}'.") { AssignedLicensePlate = existingByGps.LicensePlate.Value };
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
        await SaveAsync(vehicle.LicensePlate.Value, vehicle.GpsDeviceId, cancellationToken);
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
                $"GPS device '{gpsDevice.Value}' is already assigned to vehicle '{assignedVehicle.LicensePlate.Value}'.") { AssignedLicensePlate = assignedVehicle.LicensePlate.Value };

        if (vehicle.GpsDeviceId != gpsDevice.Value) await tripGuard.EnsureUnlockedAsync(vehicle.Id, cancellationToken);
        vehicle.PairGpsDevice(gpsDevice.Value);

        vehicleRepository.Update(vehicle);
        await SaveAsync(vehicle.LicensePlate.Value, vehicle.GpsDeviceId, cancellationToken);
        return vehicle;
    }

    public async Task<Vehicle?> Handle(UpdateVehiclePositionCommand command, CancellationToken cancellationToken = default)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(command.VehicleId, cancellationToken)
            ?? throw new FleetException(FleetErrorCodes.VehicleNotFound,
                $"Vehicle with ID {command.VehicleId} was not found.");

        await tripGuard.EnsureUnlockedAsync(vehicle.Id, cancellationToken);
        var location = new Location(command.Latitude, command.Longitude);
        vehicle.UpdatePosition(location);

        vehicleRepository.Update(vehicle);
        await SaveAsync(vehicle.LicensePlate.Value, vehicle.GpsDeviceId, cancellationToken);
        return vehicle;
    }
    private async Task SaveAsync(string plate, string? gps, CancellationToken ct)
    {
        try { await unitOfWork.CompleteAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new FleetException(FleetErrorCodes.ConcurrentModification, "Refresh and retry after a concurrent tracking change."); }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { Number: 1062 })
        {
            var owner = string.IsNullOrWhiteSpace(gps) ? null : await vehicleRepository.FindByGpsDeviceIdAsync(gps, ct);
            if (owner is not null && owner.LicensePlate.Value != plate)
                throw new FleetException(FleetErrorCodes.DeviceAlreadyAssigned, "GPS device is already assigned.") { AssignedLicensePlate = owner.LicensePlate.Value };
            throw new FleetException(FleetErrorCodes.DuplicateLicensePlate, "License plate is already registered.");
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { Number: 1205 or 1213 })
        { throw new FleetException(FleetErrorCodes.ConcurrentModification, "A concurrent write prevented this change. Refresh and retry."); }
    }
}
