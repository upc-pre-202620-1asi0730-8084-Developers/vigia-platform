using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

// EstimatedTime uses the format "HH:mm".
public record CreateDispatchCommand(
    string ProjectName,
    string SiteId,
    string Address,
    DateOnly DepartureDate,
    string EstimatedTime,
    string RelatedOrderId,
    DispatchType DispatchType,
    DispatchPriority Priority,
    string CreatedBy,
    string InternalReference,
    string? Observations,
    IReadOnlyList<CreateDispatchItemCommand> Items);
