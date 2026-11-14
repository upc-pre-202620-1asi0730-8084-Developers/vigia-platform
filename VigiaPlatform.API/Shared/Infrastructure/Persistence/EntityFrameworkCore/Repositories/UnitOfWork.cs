namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class UnitOfWork(AppDbContext context, IEnumerable<IChangeRecorder> recorders) : IUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken) : null;
        var writers = recorders.ToArray();
        foreach (var writer in writers) writer.CaptureChanges();
        await context.SaveChangesAsync(cancellationToken);
        foreach (var writer in writers) writer.AppendChanges();
        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }
}
