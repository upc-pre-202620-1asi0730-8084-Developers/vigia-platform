namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public class GeofenceConfiguration : IEntityTypeConfiguration<Geofence>
{
    public void Configure(EntityTypeBuilder<Geofence> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(g => g.CompanyId).IsRequired().HasMaxLength(64);
        builder.Property(g => g.SiteId).IsRequired().HasMaxLength(64);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(150);
        builder.Property(g => g.Type).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(g => g.RadiusMeters).IsRequired();

        builder.OwnsOne(g => g.CenterPoint, cp =>
        {
            cp.WithOwner().HasForeignKey("Id");
            cp.Property(p => p.Latitude).HasColumnName("CenterLatitude").IsRequired();
            cp.Property(p => p.Longitude).HasColumnName("CenterLongitude").IsRequired();
        });
        builder.Navigation(g => g.CenterPoint).IsRequired();
    }
}