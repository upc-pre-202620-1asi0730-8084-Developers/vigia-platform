namespace VigiaPlatform.API.TransitTraceability.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;
public class VehicleTripGuard(AppDbContext db) : IVehicleTripGuard
{
    public async Task EnsureUnlockedAsync(int vehicleId, CancellationToken ct)
    {
        if (await db.Set<DispatchTrip>().AnyAsync(t => t.ActiveVehicleId == vehicleId, ct))
            throw new FleetException("FLEET_VEHICLE_IN_TRANSIT", "GPS and manual position changes are locked during an active trip.");
    }
}
