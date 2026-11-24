using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class DispatchWeighingConfiguration : IEntityTypeConfiguration<DispatchWeighing>
{
    public void Configure(EntityTypeBuilder<DispatchWeighing> builder)
    {
        builder.ToTable("dispatch_weighings");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.WeightKg).HasPrecision(12, 3);
        builder.Property(w => w.ExpectedWeightKg).HasPrecision(12, 3);
        builder.Property(w => w.ToleranceKg).HasPrecision(12, 3);
        builder.Property(w => w.ComparisonStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(w => w.VehiclePlate).HasMaxLength(20);
        builder.Property(w => w.CargoFingerprint).HasMaxLength(64);
        builder.Property(w => w.RecordedBy).HasMaxLength(150);
        builder.Property(w => w.RequestId).HasMaxLength(36);
        builder.Property(w => w.WeighedAt).HasConversion(v => v.UtcDateTime,
            v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));
        builder.HasIndex(w => new { w.DispatchId, w.RequestId }).IsUnique();
        builder.Ignore(w => w.DifferenceKg);
        builder.Ignore(w => w.RequiresReview);
    }
}
