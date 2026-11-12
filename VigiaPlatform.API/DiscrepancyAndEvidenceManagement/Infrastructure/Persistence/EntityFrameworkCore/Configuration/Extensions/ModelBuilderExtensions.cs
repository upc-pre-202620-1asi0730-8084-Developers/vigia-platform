using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyDiscrepancyAndEvidenceManagementConfiguration(
        this ModelBuilder builder)
    {
        builder.Entity<DiscrepancyCase>(entity =>
        {
            entity.ToTable("discrepancy_cases");

            entity.HasKey(discrepancy => discrepancy.Id);

            entity.Property(discrepancy => discrepancy.Id)
                .ValueGeneratedOnAdd();

            entity.Property(discrepancy => discrepancy.DispatchId)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(discrepancy => discrepancy.ReceptionId)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(discrepancy => discrepancy.CaseNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(discrepancy => discrepancy.CaseNumber)
                .IsUnique();

            entity.Property(discrepancy => discrepancy.OpenedAt)
                .IsRequired();

            entity.Property(discrepancy => discrepancy.ClosedAt);

            entity.Property(discrepancy => discrepancy.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(discrepancy => discrepancy.DeterminedLiability)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(discrepancy => discrepancy.ResolutionSummary)
                .HasMaxLength(1000);

            entity.HasMany(discrepancy => discrepancy.Events)
                .WithOne()
                .HasForeignKey(discrepancyEvent =>
                    discrepancyEvent.DiscrepancyCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(discrepancy => discrepancy.Events)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<DiscrepancyCaseEvent>(entity =>
        {
            entity.ToTable("discrepancy_case_events");

            entity.HasKey(discrepancyEvent => discrepancyEvent.Id);

            entity.Property(discrepancyEvent => discrepancyEvent.Id)
                .ValueGeneratedOnAdd();

            entity.Property(discrepancyEvent => discrepancyEvent.Stage)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(discrepancyEvent => discrepancyEvent.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(discrepancyEvent => discrepancyEvent.Description)
                .HasMaxLength(1200)
                .IsRequired();

            entity.Property(discrepancyEvent => discrepancyEvent.OccurredAt)
                .IsRequired();
        });
    }
}
