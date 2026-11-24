namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class StoredEvidenceFileConfiguration : IEntityTypeConfiguration<StoredEvidenceFile>
{
    public void Configure(EntityTypeBuilder<StoredEvidenceFile> b)
    {
        b.ToTable("stored_evidence_files");b.HasKey(f=>f.Id);b.Property(f=>f.Id).ValueGeneratedNever();
        b.Property(f=>f.Content).HasColumnType("mediumblob").IsRequired();
        b.Property(f=>f.ContentType).HasMaxLength(40);b.Property(f=>f.FileName).HasMaxLength(120);
        b.Property(f=>f.Fingerprint).HasMaxLength(64);b.Property(f=>f.Sha256).HasMaxLength(64);
        b.Property(f=>f.UploadedAt).HasConversion(new UtcTimestampConverter()).HasColumnType("datetime(6)").HasPrecision(6);
        b.HasIndex(f=>new {f.ReceptionId,f.RequestId}).IsUnique();
        b.HasOne(f=>f.Evidence).WithOne().HasForeignKey<StoredEvidenceFile>(f=>f.EvidenceId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Reception>().WithMany().HasForeignKey(f=>f.ReceptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
