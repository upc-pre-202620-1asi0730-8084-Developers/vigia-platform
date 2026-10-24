namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using VigiaPlatform.API.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private static void ApplyAuditTimestamps(DbContext? context)
    {
        if (context is null) return;
        
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAt = now;
            if (entry.State is EntityState.Added) entry.Entity.CreatedAt = now;
        }
    }
}