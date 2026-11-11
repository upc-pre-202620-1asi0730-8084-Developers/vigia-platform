using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

namespace VigiaPlatform.API.TransitTraceability.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
/// Maps the Transit Traceability aggregate. It is discovered by AppDbContext through
/// ApplyConfigurationsFromAssembly, so each bounded context owns its mapping.
/// </summary>
public class TransitRouteConfiguration : IEntityTypeConfiguration<TransitRoute>
{
    public void Configure(EntityTypeBuilder<TransitRoute> builder)
    {
        builder.ToTable("routes");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.DispatchId).IsRequired().HasMaxLength(40);
        builder.HasIndex(r => r.DispatchId).IsUnique().HasDatabaseName("uq_routes_dispatch_id");
        builder.Property(r => r.VehicleId).IsRequired();
        builder.Property(r => r.VehiclePlate).IsRequired().HasMaxLength(20);
        builder.Property(r => r.OriginName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.DestinationGeofenceId).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.StartedAt).IsRequired();
        builder.Property(r => r.EstimatedArrival).IsRequired();
        builder.Property(r => r.DeviationThresholdMeters).IsRequired();
        builder.Property(r => r.StopThresholdMinutes).IsRequired();

        builder.ComplexProperty(r => r.Destination, destination =>
        {
            destination.Property(d => d.Name).HasColumnName("destination_name").IsRequired().HasMaxLength(150);
            destination.Property(d => d.Latitude).HasColumnName("destination_latitude").IsRequired();
            destination.Property(d => d.Longitude).HasColumnName("destination_longitude").IsRequired();
            destination.Property(d => d.RadiusMeters).HasColumnName("destination_radius_meters").IsRequired();
        });
        
        builder.HasMany(r => r.Waypoints).WithOne().HasForeignKey(w => w.RouteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Waypoints).HasField("_waypoints").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Positions).WithOne().HasForeignKey(p => p.RouteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Positions).HasField("_positions").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Alerts).WithOne().HasForeignKey(a => a.RouteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Alerts).HasField("_alerts").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.IsActive);
        builder.Ignore(r => r.CurrentPosition);
        builder.Ignore(r => r.PendingNotifications);
        builder.Ignore(r => r.TravelledDistanceMeters);
    }
}

public class RouteWaypointConfiguration : IEntityTypeConfiguration<RouteWaypoint>
{
    public void Configure(EntityTypeBuilder<RouteWaypoint> builder)
    {
        builder.ToTable("route_waypoints");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedOnAdd();
        builder.Property(w => w.RouteId).IsRequired();
        builder.Property(w => w.Sequence).IsRequired();
        builder.Property(w => w.Latitude).IsRequired();
        builder.Property(w => w.Longitude).IsRequired();
        builder.Ignore(w => w.Point);
    }
}

public class TelemetryPositionConfiguration : IEntityTypeConfiguration<TelemetryPosition>
{
    public void Configure(EntityTypeBuilder<TelemetryPosition> builder)
    {
        builder.ToTable("telemetry_positions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.RouteId).IsRequired();
        builder.Property(p => p.Latitude).IsRequired();
        builder.Property(p => p.Longitude).IsRequired();
        builder.Property(p => p.SpeedKmh).IsRequired();
        builder.Property(p => p.ReportedAt).IsRequired();
        builder.HasIndex(p => new { p.RouteId, p.ReportedAt }).HasDatabaseName("ix_telemetry_positions_route_id_reported_at");
        builder.Ignore(p => p.Point);
    }
}

public class TransitAlertConfiguration : IEntityTypeConfiguration<TransitAlert>
{
    public void Configure(EntityTypeBuilder<TransitAlert> builder)
    {
        builder.ToTable("transit_alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();
        builder.Property(a => a.RouteId).IsRequired();
        builder.Property(a => a.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.DetectedAt).IsRequired();
        builder.Property(a => a.Latitude);
        builder.Property(a => a.Longitude);
        builder.Property(a => a.Value);
        builder.Property(a => a.NotifiedAt);
        builder.Ignore(a => a.RequiresNotification);
        builder.Ignore(a => a.IsPendingNotification);
    }
}
