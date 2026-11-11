using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Maps the Dispatch aggregate (dispatches, dispatch items and timeline events) to the database.
    ///     Value objects are persisted as plain columns through value converters.
    /// </summary>
    public static void ApplyOrderManagementAndDispatchConfiguration(this ModelBuilder builder)
    {
        ConfigureDispatch(builder);
        ConfigureDispatchItem(builder);
        ConfigureTimelineEvent(builder);
    }

    private static void ConfigureDispatch(ModelBuilder builder)
    {
        var dispatch = builder.Entity<Dispatch>();

        dispatch.ToTable("dispatches");
        dispatch.HasKey(d => d.Id);
        dispatch.Property(d => d.Id).ValueGeneratedOnAdd();

        dispatch.Property(d => d.ProjectName)
            .HasConversion(v => v.Value, v => new DispatchProjectName(v))
            .HasMaxLength(100).IsRequired();

        dispatch.Property(d => d.SiteId)
            .HasConversion(v => v.Value, v => new DispatchSiteId(v))
            .HasMaxLength(50).IsRequired();

        dispatch.Property(d => d.Address)
            .HasConversion(v => v.Value, v => new DispatchAddress(v))
            .HasMaxLength(200).IsRequired();

        // DateOnly is stored through DateTime to stay compatible with the MySQL provider.
        dispatch.Property(d => d.DepartureDate)
            .HasConversion(
                v => v.Value.ToDateTime(TimeOnly.MinValue),
                v => new DispatchApertureDate(DateOnly.FromDateTime(v)))
            .HasColumnType("date").IsRequired();

        dispatch.Property(d => d.EstimatedTime)
            .HasConversion(v => v.ToString(), v => new DispatchEstimatedTime(v))
            .HasMaxLength(5).IsRequired();

        dispatch.Property(d => d.RelatedOrderId)
            .HasConversion(v => v.Value, v => new RelatedOrderId(v))
            .HasMaxLength(50).IsRequired();

        dispatch.Property(d => d.DispatchType).HasConversion<string>().HasMaxLength(30).IsRequired();
        dispatch.Property(d => d.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        dispatch.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        dispatch.Property(d => d.CreatedBy)
            .HasConversion(v => v.Value, v => new DispatchCreatedBy(v))
            .HasMaxLength(100).IsRequired();

        dispatch.Property(d => d.CreationDate)
            .HasConversion(v => v.Value, v => new DispatchCreatedAt(v))
            .IsRequired();

        dispatch.Property(d => d.InternalReference)
            .HasConversion(v => v.Value, v => new InternalReference(v))
            .HasMaxLength(50).IsRequired();

        dispatch.Property(d => d.Observations)
            .HasConversion(v => v.Value, v => new DispatchObservations(v))
            .HasMaxLength(500).IsRequired();

        // Optional: a dispatch may not have a vehicle assigned yet.
        dispatch.Property(d => d.VehicleId)
            .HasConversion(new ValueConverter<VehicleId, int>(v => v.Value, v => new VehicleId(v)));

        dispatch.Property(d => d.VehiclePlate)
            .HasConversion(v => v!.Value, v => new VehiclePlate(v))
            .HasMaxLength(20);

        dispatch.Property(d => d.CreatedAt);
        dispatch.Property(d => d.UpdatedAt);

        dispatch.HasIndex(d => d.InternalReference).IsUnique();
        dispatch.HasIndex(d => d.Status);
        dispatch.HasIndex(d => d.DepartureDate);
        dispatch.HasIndex(d => d.RelatedOrderId);

        dispatch.HasMany(d => d.Items)
            .WithOne()
            .HasForeignKey(i => i.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        dispatch.HasMany(d => d.Timeline)
            .WithOne()
            .HasForeignKey(t => t.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        dispatch.Navigation(d => d.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        dispatch.Navigation(d => d.Timeline).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureDispatchItem(ModelBuilder builder)
    {
        var item = builder.Entity<DispatchItem>();

        item.ToTable("dispatch_items");
        item.HasKey(i => i.Id);
        item.Property(i => i.Id).ValueGeneratedOnAdd();
        item.Property(i => i.DispatchId).IsRequired();

        item.Property(i => i.Code)
            .HasConversion(v => v.Value, v => new Code(v))
            .HasMaxLength(50).IsRequired();

        item.Property(i => i.Material)
            .HasConversion(v => v.Value, v => new Material(v))
            .HasMaxLength(100).IsRequired();

        item.Property(i => i.Quantity)
            .HasConversion(v => v.Value, v => new MaterialQuantity(v))
            .IsRequired();

        item.Property(i => i.Unit).HasConversion<string>().HasMaxLength(10).IsRequired();

        item.Property(i => i.Observation)
            .HasConversion(v => v.Value, v => new ItemObservation(v))
            .HasMaxLength(250).IsRequired();
    }

    private static void ConfigureTimelineEvent(ModelBuilder builder)
    {
        var timelineEvent = builder.Entity<TimelineEvent>();

        timelineEvent.ToTable("dispatch_timeline_events");
        timelineEvent.HasKey(t => t.Id);
        timelineEvent.Property(t => t.Id).ValueGeneratedOnAdd();
        timelineEvent.Property(t => t.DispatchId).IsRequired();

        timelineEvent.Property(t => t.Event)
            .HasConversion(v => v.Value, v => new TimelineEventName(v))
            .HasColumnName("event_name")
            .HasMaxLength(100).IsRequired();

        timelineEvent.Property(t => t.User)
            .HasConversion(v => v.Value, v => new TimelineEventUser(v))
            .HasMaxLength(100).IsRequired();

        timelineEvent.Property(t => t.Description)
            .HasConversion(v => v.Value, v => new TimelineEventDescription(v))
            .HasMaxLength(500).IsRequired();

        timelineEvent.Property(t => t.Date)
            .HasConversion(v => v.Value, v => new TimelineEventDate(v))
            .HasColumnName("event_date")
            .IsRequired();
    }
}
