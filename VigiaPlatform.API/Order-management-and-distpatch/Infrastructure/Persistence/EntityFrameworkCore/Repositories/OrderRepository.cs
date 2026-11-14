using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository
{
    // Items, attached files and timeline always travel with the aggregate.
    private IQueryable<Order> OrdersWithDetails =>
        Context.Set<Order>()
            .Include(o => o.Items)
            .Include(o => o.AttachedFiles)
            .Include(o => o.Timeline)
            .AsSplitQuery();

    public override async Task<Order?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await OrdersWithDetails.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<Order>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await OrdersWithDetails
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> FindByStatusAsync(OrderStatusType status,
        CancellationToken cancellationToken = default)
    {
        return await OrdersWithDetails
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> FindByCodeAsync(OrderCode code, CancellationToken cancellationToken = default)
    {
        return await OrdersWithDetails.FirstOrDefaultAsync(o => o.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(OrderCode code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Order>().AnyAsync(o => o.Code == code, cancellationToken);
    }
}
