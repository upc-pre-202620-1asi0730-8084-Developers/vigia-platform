namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

// Code: SOL-### | Date: yyyy-MM-dd | Priority: LOW, AVERAGE or HIGH
public record CreateOrderResource(
    string Code,
    string ProjectName,
    string RequesterName,
    string Priority,
    string Date,
    string MainMaterial,
    string? Observations,
    IReadOnlyList<CreateOrderItemResource> Items,
    IReadOnlyList<CreateOrderAttachedFileResource>? AttachedFiles);
