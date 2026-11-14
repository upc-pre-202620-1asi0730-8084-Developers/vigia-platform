using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class OrderResourceFromEntityAssembler
{
    public static OrderResource ToResourceFromEntity(Order entity) =>
        new(entity.Id,
            entity.Code.Value,
            entity.ProjectName.Value,
            entity.RequesterName.Value,
            EnumApiFormat.ToApi(entity.Status),
            EnumApiFormat.ToApi(entity.Priority),
            entity.Date.Value.ToString("yyyy-MM-dd"),
            entity.MainMaterial.Value,
            entity.Items.Select(ToResourceFromEntity).ToList(),
            entity.Observations.Value,
            entity.AttachedFiles.Select(ToResourceFromEntity).ToList(),
            entity.Timeline.Select(ToResourceFromEntity).ToList());

    public static OrderItemResource ToResourceFromEntity(OrderItem item) =>
        new(item.Id,
            item.Material.Value,
            item.Quantity.Value,
            item.Unit.ToString(),
            item.Observation.Value);

    public static OrderAttachedFileResource ToResourceFromEntity(OrderAttachedFile file) =>
        new(file.Id,
            file.Name.Value,
            EnumApiFormat.ToApi(file.Type),
            file.Size.ToString());

    public static OrderTimelineEventResource ToResourceFromEntity(OrderTimelineEvent timelineEvent) =>
        new(timelineEvent.Id,
            timelineEvent.Title.Value,
            timelineEvent.User.Value,
            timelineEvent.Description.Value,
            timelineEvent.Date.ToString());
}
