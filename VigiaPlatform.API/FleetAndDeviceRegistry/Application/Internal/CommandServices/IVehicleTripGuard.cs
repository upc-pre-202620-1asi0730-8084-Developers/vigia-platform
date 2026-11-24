namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;
public interface IVehicleTripGuard { Task EnsureUnlockedAsync(int vehicleId, CancellationToken ct); }
