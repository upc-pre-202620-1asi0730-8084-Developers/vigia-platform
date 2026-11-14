using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;

public interface IOrderQueryService
{
    Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken = default);
    Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken = default);
    Task<Order?> Handle(GetOrderByCodeQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> Handle(GetOrdersByStatusQuery query, CancellationToken cancellationToken = default);
}
