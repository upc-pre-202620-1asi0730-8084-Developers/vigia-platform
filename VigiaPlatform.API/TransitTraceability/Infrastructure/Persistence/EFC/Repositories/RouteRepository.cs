using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;
using VigiaPlatform.API.TransitTraceability.Domain.Repositories;

namespace VigiaPlatform.API.TransitTraceability.Infrastructure.Persistence.EFC.Repositories;

public class RouteRepository(AppDbContext context) : BaseRepository<TransitRoute>(context), IRouteRepository
{
    /// <summary>
    /// Routes with their children loaded in chronological order, as the aggregate expects them.
    /// </summary>
    private IQueryable<TransitRoute> WithDetails() =>
        Context.Set<TransitRoute>()
            .Include(r => r.Waypoints.OrderBy(w => w.Sequence))
            .Include(r => r.Positions.OrderBy(p => p.ReportedAt).ThenBy(p => p.Id))
            .Include(r => r.Alerts.OrderBy(a => a.DetectedAt).ThenBy(a => a.Id))
            .AsSplitQuery();

    public async Task<TransitRoute?> FindByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await WithDetails().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TransitRoute>> ListWithDetailsAsync(RouteStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = WithDetails();
        if (status is not null) query = query.Where(r => r.Status == status);
        return await query.OrderBy(r => r.Id).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByDispatchIdAsync(string dispatchId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<TransitRoute>().AnyAsync(r => r.DispatchId == dispatchId, cancellationToken);
    }
}
