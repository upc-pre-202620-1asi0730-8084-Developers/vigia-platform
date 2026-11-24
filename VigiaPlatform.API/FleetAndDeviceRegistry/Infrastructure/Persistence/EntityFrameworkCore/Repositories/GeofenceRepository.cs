namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Entities;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class GeofenceRepository(AppDbContext context) : BaseRepository<Geofence>(context), IGeofenceRepository
{
    public async Task<Geofence?> FindByCompanyAndSiteAsync(string companyId, string siteId, CancellationToken cancellationToken = default) =>
        await Context.Set<Geofence>().FirstOrDefaultAsync(g => g.CompanyId == companyId.Trim() && g.SiteId == siteId.Trim(), cancellationToken);

    public async Task AddForSiteAsync(Geofence geofence, CancellationToken cancellationToken = default)
    {
        await Context.Set<Geofence>().AddAsync(geofence, cancellationToken);
        await Context.Set<GeofenceSiteRegistration>().AddAsync(new GeofenceSiteRegistration(geofence), cancellationToken);
    }

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
