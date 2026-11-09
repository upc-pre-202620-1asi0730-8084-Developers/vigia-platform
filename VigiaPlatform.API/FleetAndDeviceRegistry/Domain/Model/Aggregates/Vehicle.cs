namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

using VigiaPlatform.API.Shared.Domain.Model;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

/// <summary>
/// Vehicle (Transport Unit) Aggregate Root for the Fleet and Device Registry Bounded Context (BC-04).
/// Enforces domain invariants from EventStorming:
/// - A transport unit must have a valid company, license plate, brand, model, and positive capacity.
/// - Telematics GPS device pairing is strictly 1:1.
/// - Position tracking updates reflect real-time telemetry coordinates.
/// </summary>
public class Vehicle : IAuditableEntity
{
    public int Id { get; private set; }
    public string CompanyId { get; private set; } = string.Empty;
    public LicensePlate LicensePlate { get; private set; } = default!;
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public VehicleCapacity MaxCapacityTons { get; private set; } = default!;
    public string? GpsDeviceId { get; private set; }
    public Location? CurrentPos { get; private set; }

    public bool IsGpsPaired => !string.IsNullOrWhiteSpace(GpsDeviceId);

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Vehicle() { }

    /// <summary>
    /// Executes the "Register transport unit" command by Warehouse Manager.
    /// </summary>
    public Vehicle(string companyId, LicensePlate licensePlate, string brand, string model, VehicleCapacity maxCapacityTons, string? gpsDeviceId = null)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            throw new FleetException(FleetErrorCodes.InvalidVehicleData, "Company identifier is required.");
        if (string.IsNullOrWhiteSpace(brand))
            throw new FleetException(FleetErrorCodes.InvalidVehicleData, "Vehicle brand is required.");
        if (string.IsNullOrWhiteSpace(model))
            throw new FleetException(FleetErrorCodes.InvalidVehicleData, "Vehicle model is required.");

        CompanyId = companyId.Trim();
        LicensePlate = licensePlate ?? throw new FleetException(FleetErrorCodes.InvalidLicensePlate, "License plate is required.");
        Brand = brand.Trim();
        Model = model.Trim();
        MaxCapacityTons = maxCapacityTons ?? throw new FleetException(FleetErrorCodes.InvalidCapacity, "Max capacity is required.");

        if (!string.IsNullOrWhiteSpace(gpsDeviceId))
        {
            PairGpsDevice(gpsDeviceId);
        }
    }

    /// <summary>
    /// Executes the "Link an available GPS device to the unit" command.
    /// Enforces the business policy that links a telematics tracker.
    /// </summary>
    public void PairGpsDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new FleetException(FleetErrorCodes.InvalidGpsDevice, "GPS Device ID cannot be empty.");

        var normalizedId = deviceId.Trim().ToUpperInvariant();
        if (IsGpsPaired && GpsDeviceId == normalizedId)
            return; // Idempotent if same device

        GpsDeviceId = normalizedId;
    }

    /// <summary>
    /// Unpairs the current telematics GPS device from this vehicle.
    /// </summary>
    public void UnpairGpsDevice()
    {
        GpsDeviceId = null;
    }

    /// <summary>
    /// Updates the current telemetry position of the vehicle (Location WGS84).
    /// </summary>
    public void UpdatePosition(Location location)
    {
        CurrentPos = location ?? throw new FleetException(FleetErrorCodes.InvalidCoordinates, "Position location cannot be null.");
    }
}
