using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class DispatchDeliveryGuideConfiguration : IEntityTypeConfiguration<DispatchDeliveryGuide>
{
    public void Configure(EntityTypeBuilder<DispatchDeliveryGuide> builder)
    {
        builder.ToTable("dispatch_delivery_guides");
        builder.HasKey(g => g.DispatchId);
        builder.Property(g => g.DispatchId).ValueGeneratedNever();
        builder.Property(g => g.GuideNumber).HasMaxLength(50);
        builder.HasIndex(g => g.GuideNumber).IsUnique();
        builder.Property(g => g.ProjectName).HasMaxLength(100);
        builder.Property(g => g.SiteId).HasMaxLength(50);
        builder.Property(g => g.Address).HasMaxLength(200);
        builder.Property(g => g.RelatedOrderId).HasMaxLength(50);
        builder.Property(g => g.DriverName).HasMaxLength(150);
        builder.Property(g => g.CarrierName).HasMaxLength(150);
        builder.Property(g => g.VehiclePlate).HasMaxLength(20);
        builder.Property(g => g.CargoFingerprint).HasMaxLength(64);
        builder.Property(g => g.ItemsJson).HasColumnType("longtext");
        builder.Property(g => g.IssuedBy).HasMaxLength(150);
        builder.Property(g => g.RequestId).HasMaxLength(36);
        builder.Property(g => g.WeightKg).HasPrecision(12, 3);
        builder.Property(g => g.ExpectedWeightKg).HasPrecision(12, 3);
        builder.Property(g => g.ToleranceKg).HasPrecision(12, 3);
        builder.Property(g => g.IssuedAt).HasConversion(v => v.UtcDateTime,
            v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));
        builder.Ignore(g => g.Items);
    }
}
