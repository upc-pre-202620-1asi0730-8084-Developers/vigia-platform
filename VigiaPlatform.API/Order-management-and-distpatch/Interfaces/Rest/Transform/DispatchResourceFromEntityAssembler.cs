using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class DispatchResourceFromEntityAssembler
{
    public static DispatchResource ToResourceFromEntity(Dispatch entity) =>
        new(entity.Id,
            $"DS-{entity.Id}",
            entity.ProjectName.Value,
            entity.SiteId.Value,
            entity.Address.Value,
            entity.DepartureDate.ToString(),
            entity.EstimatedTime.ToString(),
            entity.RelatedOrderId.Value,
            EnumApiFormat.ToApi(entity.DispatchType),
            EnumApiFormat.ToApi(entity.Priority),
            EnumApiFormat.ToApi(entity.Status),
            entity.CreatedBy.Value,
            entity.CreationDate.ToString(),
            entity.InternalReference.Value,
            entity.Observations.Value,
            entity.Items.Select(ToResourceFromEntity).ToList(),
            entity.Timeline.Select(ToResourceFromEntity).ToList(),
            entity.VehicleId?.Value,
            entity.VehiclePlate?.Value);

    public static DispatchItemResource ToResourceFromEntity(DispatchItem item) =>
        new(item.Id,
            item.Code.Value,
            item.Material.Value,
            item.Quantity.Value,
            item.Unit.ToString(),
            item.Observation.Value);

    public static TimelineEventResource ToResourceFromEntity(TimelineEvent timelineEvent) =>
        new(timelineEvent.Id,
            timelineEvent.Event.Value,
            timelineEvent.User.Value,
            timelineEvent.Description.Value,
            timelineEvent.Date.ToString());
}
