using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;

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
                .ValueGeneratedNever();

            entity.Property(discrepancy => discrepancy.DispatchId)
                .IsRequired();

            entity.Property(discrepancy => discrepancy.ReceptionId)
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
        });
    }
}