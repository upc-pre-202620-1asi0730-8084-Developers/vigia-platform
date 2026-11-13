namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(v => v.CompanyId).IsRequired().HasMaxLength(64);
        builder.Property(v => v.Brand).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Model).IsRequired().HasMaxLength(100);
        builder.Property(v => v.GpsDeviceId).HasMaxLength(64);
        builder.HasIndex(v => v.GpsDeviceId).IsUnique();
        builder.Ignore(v => v.IsGpsPaired);

        builder.OwnsOne(v => v.LicensePlate, lp =>
        {
            lp.WithOwner().HasForeignKey("Id");
            lp.Property(p => p.Value).HasColumnName("LicensePlate").IsRequired().HasMaxLength(12);
            lp.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(v => v.MaxCapacityTons, cap =>
        {
            cap.WithOwner().HasForeignKey("Id");
            cap.Property(c => c.Tons).HasColumnName("MaxCapacityTons").IsRequired();
        });

        builder.OwnsOne(v => v.CurrentPos, pos =>
        {
            pos.WithOwner().HasForeignKey("Id");
            pos.Property(p => p.Latitude).HasColumnName("CurrentPosLatitude");
            pos.Property(p => p.Longitude).HasColumnName("CurrentPosLongitude");
        });
    }
}