using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class DiscrepancyCaseRepository(AppDbContext context)
    : IDiscrepancyCaseRepository
{
    public async Task<IEnumerable<DiscrepancyCase>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Set<DiscrepancyCase>()
            .AsNoTracking()
            .OrderByDescending(discrepancy => discrepancy.OpenedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        DiscrepancyCase discrepancyCase,
        CancellationToken cancellationToken = default)
    {
        await context.Set<DiscrepancyCase>()
            .AddAsync(discrepancyCase, cancellationToken);
    }
}