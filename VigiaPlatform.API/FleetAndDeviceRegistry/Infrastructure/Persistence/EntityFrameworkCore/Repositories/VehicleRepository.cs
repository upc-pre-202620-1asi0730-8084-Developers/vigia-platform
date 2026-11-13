namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class VehicleRepository(AppDbContext context) : BaseRepository<Vehicle>(context), IVehicleRepository
{
    public async Task<Vehicle?> FindByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default)
    {
        var normalized = LicensePlate.Normalize(licensePlate);
        return await Context.Set<Vehicle>()
            .FirstOrDefaultAsync(v => v.LicensePlate.Value == normalized, cancellationToken);
    }

    public async Task<Vehicle?> FindByGpsDeviceIdAsync(string gpsDeviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gpsDeviceId)) return null;
        var normalized = gpsDeviceId.Trim().ToUpperInvariant();
        return await Context.Set<Vehicle>()
            .FirstOrDefaultAsync(v => v.GpsDeviceId == normalized, cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> ListByGpsPairedStatusAsync(bool isGpsPaired, CancellationToken cancellationToken = default)
    {
        return isGpsPaired
            ? await Context.Set<Vehicle>()
                .Where(v => v.GpsDeviceId != null && v.GpsDeviceId != "")
                .ToListAsync(cancellationToken)
            : await Context.Set<Vehicle>()
                .Where(v => v.GpsDeviceId == null || v.GpsDeviceId == "")
                .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> ListByCompanyIdAsync(string companyId, CancellationToken cancellationToken = default)
    {
        var normalized = companyId.Trim();
        return await Context.Set<Vehicle>()
            .Where(v => v.CompanyId == normalized)
            .ToListAsync(cancellationToken);
    }
}
