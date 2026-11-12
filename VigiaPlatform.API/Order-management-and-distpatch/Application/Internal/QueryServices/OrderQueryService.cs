using VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.QueryServices;

public class OrderQueryService(IOrderRepository orderRepository) : IOrderQueryService
{
    public async Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        return await orderRepository.ListAsync(cancellationToken);
    }

    public async Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await orderRepository.FindByIdAsync(query.OrderId, cancellationToken);
    }

    public async Task<Order?> Handle(GetOrderByCodeQuery query, CancellationToken cancellationToken = default)
    {
        // An invalid code can never match an existing order.
        try
        {
            return await orderRepository.FindByCodeAsync(new OrderCode(query.Code), cancellationToken);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Order>> Handle(GetOrdersByStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        return await orderRepository.FindByStatusAsync(query.Status, cancellationToken);
    }
}
