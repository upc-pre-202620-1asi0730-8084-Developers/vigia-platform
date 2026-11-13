namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Geofence> Geofences => Set<Geofence>();

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.ApplyDiscrepancyAndEvidenceManagementConfiguration();
        builder.ApplyOrderManagementAndDispatchConfiguration();
        builder.UseSnakeCaseNamingConvention();
    }
}
