namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class GeofenceRepository(AppDbContext context) : BaseRepository<Geofence>(context), IGeofenceRepository
{
    public async Task<Geofence?> FindBySiteIdAsync(string siteId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(siteId)) return null;
        var normalized = siteId.Trim();
        return await Context.Set<Geofence>()
            .FirstOrDefaultAsync(g => g.SiteId == normalized, cancellationToken);
    }

    public async Task<IEnumerable<Geofence>> ListByTypeAsync(GeofenceType type, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Geofence>()
            .Where(g => g.Type == type)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Geofence>> ListByCompanyIdAsync(string companyId, CancellationToken cancellationToken = default)
    {
        var normalized = companyId.Trim();
        return await Context.Set<Geofence>()
            .Where(g => g.CompanyId == normalized)
            .ToListAsync(cancellationToken);
    }
}
