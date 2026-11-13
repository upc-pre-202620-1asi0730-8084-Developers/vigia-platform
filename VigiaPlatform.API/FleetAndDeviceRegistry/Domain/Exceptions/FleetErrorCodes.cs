namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;

/// <summary>
/// Standard error codes for the Fleet and Device Registry Bounded Context (BC-04).
/// </summary>
public static class FleetErrorCodes
{
    public const string InvalidVehicleData = "FLEET_INVALID_VEHICLE_DATA";
    public const string InvalidLicensePlate = "FLEET_INVALID_LICENSE_PLATE";
    public const string InvalidCapacity = "FLEET_INVALID_CAPACITY";
    public const string InvalidGpsDevice = "FLEET_INVALID_GPS_DEVICE";
    public const string DeviceAlreadyAssigned = "FLEET_DEVICE_ALREADY_ASSIGNED";
    public const string InvalidGeofence = "FLEET_INVALID_GEOFENCE";
    public const string InvalidCoordinates = "FLEET_INVALID_COORDINATES";
    public const string InvalidRadius = "FLEET_INVALID_RADIUS";
    public const string VehicleNotFound = "FLEET_VEHICLE_NOT_FOUND";
    public const string GeofenceNotFound = "FLEET_GEOFENCE_NOT_FOUND";
    public const string DuplicateLicensePlate = "FLEET_DUPLICATE_LICENSE_PLATE";
}
