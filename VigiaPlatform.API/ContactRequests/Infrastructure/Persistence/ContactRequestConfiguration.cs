namespace VigiaPlatform.API.ContactRequests.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates;

public sealed class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> builder)
    {
        builder.ToTable("contact_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RequestId).HasMaxLength(64);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(180);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Segment).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Plan).HasMaxLength(40);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(1500);
        builder.Property(x => x.Locale).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.RequestId).IsUnique();
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.Email);
    }
}
