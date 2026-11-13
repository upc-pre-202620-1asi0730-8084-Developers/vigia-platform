using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;

public interface IOrderRepository : IBaseRepository<Order>
{
    Task<IEnumerable<Order>> FindByStatusAsync(OrderStatusType status,
        CancellationToken cancellationToken = default);

    Task<Order?> FindByCodeAsync(OrderCode code, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(OrderCode code, CancellationToken cancellationToken = default);
}
