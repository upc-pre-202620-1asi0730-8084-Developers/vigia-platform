using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class OrderCommandFromResourceAssembler
{
    public static AddOrderAttachedFileCommand ToCommandFromResource(int orderId,
        AddOrderAttachedFileResource resource) =>
        new(orderId,
            resource.Name,
            EnumApiFormat.FromApi<AttachedFileType>(resource.Type, nameof(resource.Type)),
            resource.SizeInBytes,
            resource.PerformedBy);

    public static ChangeOrderStatusCommand ToCommandFromResource(int orderId, ChangeOrderStatusResource resource) =>
        new(orderId,
            EnumApiFormat.FromApi<OrderStatusType>(resource.Status, nameof(resource.Status)),
            resource.PerformedBy,
            resource.Description);
}
