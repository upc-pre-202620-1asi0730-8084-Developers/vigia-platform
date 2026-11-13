namespace VigiaPlatform.API.ActivityHistory.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.ActivityHistory.Domain.Model;

public class ActivityConfiguration : IEntityTypeConfiguration<ActivityRecord>
{
    public void Configure(EntityTypeBuilder<ActivityRecord> builder)
    {
        builder.ToTable("activity_records");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.OccurredAt);
        builder.Property(a => a.Type).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Detail).HasMaxLength(500).IsRequired();
        builder.Property(a => a.UserName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Reference).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(30).IsRequired();
    }
}
