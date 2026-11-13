namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

using Microsoft.EntityFrameworkCore;
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

        builder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).IsRequired().ValueGeneratedOnAdd();
            entity.Property(v => v.CompanyId).IsRequired().HasMaxLength(64);
            entity.Property(v => v.Brand).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Model).IsRequired().HasMaxLength(100);
            entity.Property(v => v.GpsDeviceId).HasMaxLength(64);
            entity.HasIndex(v => v.GpsDeviceId).IsUnique();
            entity.Ignore(v => v.IsGpsPaired);

            entity.OwnsOne(v => v.LicensePlate, lp =>
            {
                lp.WithOwner().HasForeignKey("Id");
                lp.Property(p => p.Value).HasColumnName("LicensePlate").IsRequired().HasMaxLength(12);
                lp.HasIndex(p => p.Value).IsUnique();
            });

            entity.OwnsOne(v => v.MaxCapacityTons, cap =>
            {
                cap.WithOwner().HasForeignKey("Id");
                cap.Property(c => c.Tons).HasColumnName("MaxCapacityTons").IsRequired();
            });

            entity.OwnsOne(v => v.CurrentPos, pos =>
            {
                pos.WithOwner().HasForeignKey("Id");
                pos.Property(p => p.Latitude).HasColumnName("CurrentPosLatitude");
                pos.Property(p => p.Longitude).HasColumnName("CurrentPosLongitude");
            });
        });

        builder.Entity<Geofence>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).IsRequired().ValueGeneratedOnAdd();
            entity.Property(g => g.CompanyId).IsRequired().HasMaxLength(64);
            entity.Property(g => g.SiteId).IsRequired().HasMaxLength(64);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(150);
            entity.Property(g => g.Type).IsRequired().HasConversion<string>().HasMaxLength(32);
            entity.Property(g => g.RadiusMeters).IsRequired();

            entity.OwnsOne(g => g.CenterPoint, cp =>
            {
                cp.WithOwner().HasForeignKey("Id");
                cp.Property(p => p.Latitude).HasColumnName("CenterLatitude").IsRequired();
                cp.Property(p => p.Longitude).HasColumnName("CenterLongitude").IsRequired();
            });
            entity.Navigation(g => g.CenterPoint).IsRequired();
        });

        builder.UseSnakeCaseNamingConvention();
    }
}
