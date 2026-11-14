namespace VigiaPlatform.API.ActivityHistory.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.ActivityHistory.Domain.Model;
using VigiaPlatform.API.ActivityHistory.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class ActivityRepository(AppDbContext context) : IActivityRepository
{
    public async Task<IReadOnlyList<ActivityRecord>> ListAsync(CancellationToken cancellationToken) =>
        await context.Set<ActivityRecord>().AsNoTracking().OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id).ToListAsync(cancellationToken);
}
