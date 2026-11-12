using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;

public record GetOrdersByStatusQuery(OrderStatusType Status);
