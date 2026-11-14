namespace VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Entities;

public class UnassociatedTelemetryEventConfiguration : IEntityTypeConfiguration<UnassociatedTelemetryEvent>
{
    public void Configure(EntityTypeBuilder<UnassociatedTelemetryEvent> builder)
    {
        builder.ToTable("unassociated_telemetry_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GpsDeviceId).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => new { e.GpsDeviceId, e.ReportedAt });
    }
}
