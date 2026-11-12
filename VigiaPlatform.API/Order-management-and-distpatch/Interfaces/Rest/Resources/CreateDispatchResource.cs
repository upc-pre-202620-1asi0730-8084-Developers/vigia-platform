namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

// DepartureDate: yyyy-MM-dd | EstimatedTime: HH:mm | DispatchType / Priority: UPPER_SNAKE_CASE
public record CreateDispatchResource(
    string ProjectName,
    string SiteId,
    string Address,
    string DepartureDate,
    string EstimatedTime,
    string RelatedOrderId,
    string DispatchType,
    string Priority,
    string CreatedBy,
    string InternalReference,
    string? Observations,
    IReadOnlyList<CreateDispatchItemResource> Items);
