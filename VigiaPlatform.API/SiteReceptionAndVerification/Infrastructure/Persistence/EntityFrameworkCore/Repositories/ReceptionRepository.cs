namespace VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Repositories;

public class ReceptionRepository(AppDbContext context) : BaseRepository<Reception>(context), IReceptionRepository
{
    public new async Task<Reception?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reception>()
            .Include(r => r.Items)
            .Include(r => r.Evidences)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Reception?> FindByDispatchIdAsync(string dispatchId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reception>()
            .Include(r => r.Items)
            .Include(r => r.Evidences)
            .FirstOrDefaultAsync(r => r.DispatchId == dispatchId, cancellationToken);
    }

    public async Task<IEnumerable<Reception>> ListBySiteIdAsync(int siteId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reception>()
            .Include(r => r.Items)
            .Include(r => r.Evidences)
            .Where(r => r.SiteId == siteId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reception>> ListUpcomingArrivalsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reception>()
            .Include(r => r.Items)
            .Include(r => r.Evidences)
            .Where(r => r.Status == ReceptionStatus.UnitArrived)
            .ToListAsync(cancellationToken);
    }

    public new async Task<IEnumerable<Reception>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reception>()
            .Include(r => r.Items)
            .Include(r => r.Evidences)
            .ToListAsync(cancellationToken);
    }
}
