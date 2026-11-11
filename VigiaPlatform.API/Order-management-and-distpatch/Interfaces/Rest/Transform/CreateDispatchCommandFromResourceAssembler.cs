using System.Globalization;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class CreateDispatchCommandFromResourceAssembler
{
    public static CreateDispatchCommand ToCommandFromResource(CreateDispatchResource resource)
    {
        if (!DateOnly.TryParseExact(resource.DepartureDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var departureDate))
            throw new ArgumentException("Departure date must have the format yyyy-MM-dd", nameof(resource));

        var items = (resource.Items ?? [])
            .Select(i => new CreateDispatchItemCommand(
                i.Code,
                i.Material,
                i.Quantity,
                EnumApiFormat.FromApi<UnitType>(i.Unit, nameof(i.Unit)),
                i.Observation))
            .ToList();

        return new CreateDispatchCommand(
            resource.ProjectName,
            resource.SiteId,
            resource.Address,
            departureDate,
            resource.EstimatedTime,
            resource.RelatedOrderId,
            EnumApiFormat.FromApi<DispatchType>(resource.DispatchType, nameof(resource.DispatchType)),
            EnumApiFormat.FromApi<DispatchPriority>(resource.Priority, nameof(resource.Priority)),
            resource.CreatedBy,
            resource.InternalReference,
            resource.Observations,
            items);
    }
}
