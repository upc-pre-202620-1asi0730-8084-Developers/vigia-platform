using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class DispatchRepository(AppDbContext context) : BaseRepository<Dispatch>(context), IDispatchRepository
{
    // Items and timeline always travel with the aggregate.
    private IQueryable<Dispatch> DispatchesWithDetails =>
        Context.Set<Dispatch>()
            .Include(d => d.Items)
            .Include(d => d.Timeline)
            .AsSplitQuery();

    public override async Task<Dispatch?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DispatchesWithDetails.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<Dispatch>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await DispatchesWithDetails
            .OrderByDescending(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Dispatch>> FindByStatusAsync(DispatchStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DispatchesWithDetails
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Dispatch>> FindByDepartureDateAsync(DispatchApertureDate departureDate,
        CancellationToken cancellationToken = default)
    {
        return await DispatchesWithDetails
            .Where(d => d.DepartureDate == departureDate)
            .OrderByDescending(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Dispatch>> FindByRelatedOrderIdAsync(RelatedOrderId relatedOrderId,
        CancellationToken cancellationToken = default)
    {
        return await DispatchesWithDetails
            .Where(d => d.RelatedOrderId == relatedOrderId)
            .OrderByDescending(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByInternalReferenceAsync(InternalReference internalReference,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Dispatch>()
            .AnyAsync(d => d.InternalReference == internalReference, cancellationToken);
    }
}
