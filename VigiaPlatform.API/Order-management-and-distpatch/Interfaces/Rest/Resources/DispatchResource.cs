namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record DispatchResource(
    int Id,
    string Code,
    string ProjectName,
    string SiteId,
    string Address,
    string DepartureDate,
    string EstimatedTime,
    string RelatedOrderId,
    string DispatchType,
    string Priority,
    string Status,
    string CreatedBy,
    string CreatedAt,
    string InternalReference,
    string Observations,
    IReadOnlyList<DispatchItemResource> Items,
    IReadOnlyList<TimelineEventResource> Timeline,
    int? VehicleId,
    string? VehiclePlate);
