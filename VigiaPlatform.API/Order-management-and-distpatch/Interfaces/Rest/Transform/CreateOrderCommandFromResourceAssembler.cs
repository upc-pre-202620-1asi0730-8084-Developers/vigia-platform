using System.Globalization;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class CreateOrderCommandFromResourceAssembler
{
    public static CreateOrderCommand ToCommandFromResource(CreateOrderResource resource)
    {
        if (!DateOnly.TryParseExact(resource.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            throw new ArgumentException("Date must have the format yyyy-MM-dd", nameof(resource));

        var items = (resource.Items ?? [])
            .Select(i => new CreateOrderItemCommand(
                i.Material,
                i.Quantity,
                EnumApiFormat.FromApi<ItemsUnitType>(i.Unit, nameof(i.Unit)),
                i.Observation))
            .ToList();

        var attachedFiles = (resource.AttachedFiles ?? [])
            .Select(f => new CreateOrderAttachedFileCommand(
                f.Name,
                EnumApiFormat.FromApi<AttachedFileType>(f.Type, nameof(f.Type)),
                f.SizeInBytes))
            .ToList();

        return new CreateOrderCommand(
            resource.Code,
            resource.ProjectName,
            resource.RequesterName,
            EnumApiFormat.FromApi<OrderPriorityType>(resource.Priority, nameof(resource.Priority)),
            date,
            resource.MainMaterial,
            resource.Observations,
            items,
            attachedFiles);
    }
}
