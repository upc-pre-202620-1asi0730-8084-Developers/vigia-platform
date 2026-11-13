using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class OrderModelBuilderExtensions
{
    /// <summary>
    ///     Maps the Order aggregate (orders, items, attached files and timeline events) to the database.
    ///     Value objects are persisted as plain columns through value converters.
    /// </summary>
    public static void ApplyOrderConfiguration(this ModelBuilder builder)
    {
        ConfigureOrder(builder);
        ConfigureOrderItem(builder);
        ConfigureOrderAttachedFile(builder);
        ConfigureOrderTimelineEvent(builder);
    }

    private static void ConfigureOrder(ModelBuilder builder)
    {
        var order = builder.Entity<Order>();

        order.ToTable("orders");
        order.HasKey(o => o.Id);
        order.Property(o => o.Id).ValueGeneratedOnAdd();

        order.Property(o => o.Code)
            .HasConversion(v => v.Value, v => new OrderCode(v))
            .HasMaxLength(20).IsRequired();

        order.Property(o => o.ProjectName)
            .HasConversion(v => v.Value, v => new ProjectName(v))
            .HasMaxLength(100).IsRequired();

        order.Property(o => o.RequesterName)
            .HasConversion(v => v.Value, v => new OrderRequesterName(v))
            .HasMaxLength(100).IsRequired();

        order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        order.Property(o => o.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();

        order.Property(o => o.Date)
            .HasConversion(v => v.Value, v => new OrderDate(v))
            .HasColumnType("date").IsRequired();

        order.Property(o => o.MainMaterial)
            .HasConversion(v => v.Value, v => new OrderMainMaterial(v))
            .HasMaxLength(100).IsRequired();

        order.Property(o => o.Observations)
            .HasConversion(v => v.Value, v => new OrderObservation(v))
            .HasMaxLength(350).IsRequired();

        order.Property(o => o.CreatedAt);
        order.Property(o => o.UpdatedAt);

        order.HasIndex(o => o.Code).IsUnique();
        order.HasIndex(o => o.Status);

        order.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        order.HasMany(o => o.AttachedFiles)
            .WithOne()
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        order.HasMany(o => o.Timeline)
            .WithOne()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        order.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        order.Navigation(o => o.AttachedFiles).UsePropertyAccessMode(PropertyAccessMode.Field);
        order.Navigation(o => o.Timeline).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureOrderItem(ModelBuilder builder)
    {
        var item = builder.Entity<OrderItem>();

        item.ToTable("order_items");
        item.HasKey(i => i.Id);
        item.Property(i => i.Id).ValueGeneratedOnAdd();
        item.Property(i => i.OrderId).IsRequired();

        item.Property(i => i.Material)
            .HasConversion(v => v.Value, v => new ItemsMaterial(v))
            .HasMaxLength(100).IsRequired();

        item.Property(i => i.Quantity)
            .HasConversion(v => v.Value, v => new ItemsQuantity(v))
            .IsRequired();

        item.Property(i => i.Unit).HasConversion<string>().HasMaxLength(10).IsRequired();

        item.Property(i => i.Observation)
            .HasConversion(v => v.Value, v => new ItemsObservation(v))
            .HasMaxLength(350).IsRequired();
    }

    private static void ConfigureOrderAttachedFile(ModelBuilder builder)
    {
        var file = builder.Entity<OrderAttachedFile>();

        file.ToTable("order_attached_files");
        file.HasKey(f => f.Id);
        file.Property(f => f.Id).ValueGeneratedOnAdd();
        file.Property(f => f.OrderId).IsRequired();

        file.Property(f => f.Name)
            .HasConversion(v => v.Value, v => new AttachedFilename(v))
            .HasMaxLength(AttachedFilename.MaxLength).IsRequired();

        file.Property(f => f.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The size is stored in bytes.
        file.Property(f => f.Size)
            .HasConversion(v => v.Value, v => new AttachedFileSize(v))
            .IsRequired();
    }

    private static void ConfigureOrderTimelineEvent(ModelBuilder builder)
    {
        var timelineEvent = builder.Entity<OrderTimelineEvent>();

        timelineEvent.ToTable("order_timeline_events");
        timelineEvent.HasKey(t => t.Id);
        timelineEvent.Property(t => t.Id).ValueGeneratedOnAdd();
        timelineEvent.Property(t => t.OrderId).IsRequired();

        timelineEvent.Property(t => t.Title)
            .HasConversion(v => v.Value, v => new TimeLineTitle(v))
            .HasMaxLength(TimeLineTitle.MaxLength).IsRequired();

        timelineEvent.Property(t => t.User)
            .HasConversion(v => v.Value, v => new TimeLineUser(v))
            .HasMaxLength(100).IsRequired();

        timelineEvent.Property(t => t.Description)
            .HasConversion(v => v.Value, v => new TimeLineDescription(v))
            .HasMaxLength(TimeLineDescription.MaxLength).IsRequired();

        timelineEvent.Property(t => t.Date)
            .HasConversion(v => v.Value, v => new TimeLineDate(v))
            .HasColumnName("event_date")
            .IsRequired();
    }
}
