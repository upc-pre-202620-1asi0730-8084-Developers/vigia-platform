namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record OrderResource(
    int Id,
    string Code,
    string ProjectName,
    string RequesterName,
    string Status,
    string Priority,
    string Date,
    string MainMaterial,
    IReadOnlyList<OrderItemResource> Items,
    string Observations,
    IReadOnlyList<OrderAttachedFileResource> AttachedFiles,
    IReadOnlyList<OrderTimelineEventResource> Timeline);
