using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record CreateOrderCommand(
    string Code,
    string ProjectName,
    string RequesterName,
    OrderPriorityType Priority,
    DateOnly Date,
    string MainMaterial,
    string? Observations,
    IReadOnlyList<CreateOrderItemCommand> Items,
    IReadOnlyList<CreateOrderAttachedFileCommand> AttachedFiles);
