using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record ChangeOrderStatusCommand(
    int OrderId,
    OrderStatusType NewStatus,
    string PerformedBy,
    string? Description);
