namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Entities;

public class GeofenceSiteRegistrationConfiguration : IEntityTypeConfiguration<GeofenceSiteRegistration>
{
    public void Configure(EntityTypeBuilder<GeofenceSiteRegistration> builder)
    {
        builder.ToTable("geofence_site_registrations");
        builder.HasKey(g => new { g.CompanyId, g.SiteId });
        builder.Property(g => g.CompanyId).HasMaxLength(64);
        builder.Property(g => g.SiteId).HasMaxLength(64);
        builder.HasOne(g => g.Geofence).WithMany().HasForeignKey(g => g.GeofenceId).OnDelete(DeleteBehavior.Restrict);
    }
}
