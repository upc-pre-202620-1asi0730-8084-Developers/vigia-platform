namespace VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities;

/// <summary>
/// Configures EF Core mappings for the Reception aggregate root.
/// Discovered dynamically by AppDbContext via ApplyConfigurationsFromAssembly.
/// </summary>
public class ReceptionConfiguration : IEntityTypeConfiguration<Reception>
{
    public void Configure(EntityTypeBuilder<Reception> builder)
    {
        builder.ToTable("receptions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.DispatchId).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.DispatchId).IsUnique().HasDatabaseName("uq_receptions_dispatch_id");

        builder.Property(r => r.CarrierPlate).IsRequired().HasMaxLength(20);
        builder.Property(r => r.DriverName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.SiteId).IsRequired();
        builder.Property(r => r.SiteName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.SiteResidentName).IsRequired().HasMaxLength(150);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.ArrivalRegisteredAt).IsRequired();
        builder.Property(r => r.DeliveryNoteNumber).HasMaxLength(50);
        builder.Property(r => r.ReceiverNotes).HasMaxLength(500);
        builder.Property(r => r.VerifiedByUserId).HasMaxLength(50);
        builder.Property(r => r.ClosedAt);

        // Owned Value Object: ScaleWeight
        builder.OwnsOne(r => r.ScaleWeight, sw =>
        {
            sw.Property(w => w.GrossWeightKg).HasColumnName("scale_gross_weight_kg");
            sw.Property(w => w.TareWeightKg).HasColumnName("scale_tare_weight_kg");
            sw.Property(w => w.WeighingSlipNumber).HasColumnName("scale_weighing_slip_number").HasMaxLength(50);
            sw.Property(w => w.WeighedAt).HasColumnName("scale_weighed_at");
            sw.Ignore(w => w.NetWeightKg);
        });

        // Owned Value Object: ReceptionChecklist
        builder.OwnsOne(r => r.Checklist, cl =>
        {
            cl.Property(c => c.IsSealIntact).HasColumnName("checklist_seal_intact");
            cl.Property(c => c.IsDeliveryNotePhysicalOk).HasColumnName("checklist_delivery_note_ok");
            cl.Property(c => c.IsScaleCalibrated).HasColumnName("checklist_scale_calibrated");
            cl.Property(c => c.Remarks).HasColumnName("checklist_remarks").HasMaxLength(250);
            cl.Ignore(c => c.IsFullyCompliant);
        });

        // Owned Value Object: DigitalSignature
        builder.OwnsOne(r => r.DigitalSignature, ds =>
        {
            ds.Property(s => s.SignatureData).HasColumnName("signature_data");
            ds.Property(s => s.SignedBy).HasColumnName("signature_signed_by").HasMaxLength(150);
            ds.Property(s => s.SignedAt).HasColumnName("signature_signed_at");
        });

        // Owned Value Object: DiscrepancyDetails
        builder.OwnsOne(r => r.Discrepancy, dd =>
        {
            dd.Property(d => d.Reason).HasColumnName("discrepancy_reason").HasMaxLength(100);
            dd.Property(d => d.Description).HasColumnName("discrepancy_description").HasMaxLength(500);
            dd.Property(d => d.Severity).HasColumnName("discrepancy_severity").HasMaxLength(20);
            dd.Property(d => d.ReportedAt).HasColumnName("discrepancy_reported_at");
        });

        // One-to-many relationship: Items
        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.ReceptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);

        // One-to-many relationship: Evidences
        builder.HasMany(r => r.Evidences)
            .WithOne()
            .HasForeignKey(e => e.ReceptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Evidences).HasField("_evidences").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.HasAnyDiscrepancy);
    }
}

public class ReceptionItemConfiguration : IEntityTypeConfiguration<ReceptionItem>
{
    public void Configure(EntityTypeBuilder<ReceptionItem> builder)
    {
        builder.ToTable("reception_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.ReceptionId).IsRequired();
        builder.Property(i => i.MaterialId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.MaterialName).IsRequired().HasMaxLength(150);
        builder.Property(i => i.Unit).IsRequired().HasMaxLength(20);
        builder.Property(i => i.DispatchedQuantity).IsRequired();
        builder.Property(i => i.ReceivedQuantity).IsRequired();
        builder.Property(i => i.IsVerified).IsRequired();
        builder.Property(i => i.Comments).HasMaxLength(250);

        builder.Ignore(i => i.DiscrepancyQuantity);
        builder.Ignore(i => i.HasDiscrepancy);
    }
}

public class ReceptionEvidenceConfiguration : IEntityTypeConfiguration<ReceptionEvidence>
{
    public void Configure(EntityTypeBuilder<ReceptionEvidence> builder)
    {
        builder.ToTable("reception_evidences");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.ReceptionId).IsRequired();
        builder.Property(e => e.EvidenceType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FileUrl).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(250);
        builder.Property(e => e.AttachedAt).IsRequired();
    }
}
