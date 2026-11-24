namespace VigiaPlatform.API.TransitTraceability.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

public class DispatchTripConfiguration : IEntityTypeConfiguration<DispatchTrip>
{
    public void Configure(EntityTypeBuilder<DispatchTrip> builder)
    {
        builder.ToTable("dispatch_trips"); builder.HasKey(t => t.DispatchId);
        builder.Property(t => t.DispatchId).ValueGeneratedNever();
        builder.Property(t => t.GpsDeviceId).HasMaxLength(64);
        builder.Property(t => t.RequestFingerprint).HasMaxLength(64);
        builder.Property(t => t.StartedBy).HasMaxLength(150);
        builder.Property(t => t.EndReason).HasMaxLength(40);
        builder.Property(t => t.DepartedAt).HasConversion(new VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.UtcTimestampConverter()).HasColumnType("datetime(6)").HasPrecision(6);
        builder.Property(t => t.EndedAt).HasConversion(new VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.UtcTimestampConverter()).HasColumnType("datetime(6)").HasPrecision(6);
        builder.HasIndex(t => t.ActiveVehicleId).IsUnique();
        builder.HasOne(t => t.Route).WithOne(r => r.Trip).HasForeignKey<DispatchTrip>(t => t.RouteId).OnDelete(DeleteBehavior.Restrict);
    }
}
