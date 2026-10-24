namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}