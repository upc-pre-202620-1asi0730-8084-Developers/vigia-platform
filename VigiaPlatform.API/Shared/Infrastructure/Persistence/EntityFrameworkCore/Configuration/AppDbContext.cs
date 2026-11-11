namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.UseSnakeCaseNamingConvention();
    }
}
