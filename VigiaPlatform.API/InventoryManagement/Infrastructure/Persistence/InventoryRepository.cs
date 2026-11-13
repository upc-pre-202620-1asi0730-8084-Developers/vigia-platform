namespace VigiaPlatform.API.InventoryManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.InventoryManagement.Domain.Model;
using VigiaPlatform.API.InventoryManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class InventoryRepository(AppDbContext context) : IInventoryRepository
{
    public async Task<IReadOnlyList<Material>> GetMaterialsAsync(CancellationToken cancellationToken) =>
        await context.Set<Material>().AsNoTracking().OrderBy(m => m.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryMovement>> GetMovementsAsync(CancellationToken cancellationToken) =>
        await context.Set<InventoryMovement>().AsNoTracking().OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
}
