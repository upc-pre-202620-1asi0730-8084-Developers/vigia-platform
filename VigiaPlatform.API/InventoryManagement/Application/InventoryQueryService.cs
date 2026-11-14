namespace VigiaPlatform.API.InventoryManagement.Application;

using VigiaPlatform.API.InventoryManagement.Domain.Model;
using VigiaPlatform.API.InventoryManagement.Domain.Repositories;

public class InventoryQueryService(IInventoryRepository repository)
{
    public Task<IReadOnlyList<Material>> GetMaterialsAsync(CancellationToken cancellationToken) => repository.GetMaterialsAsync(cancellationToken);
    public Task<IReadOnlyList<InventoryMovement>> GetMovementsAsync(CancellationToken cancellationToken) => repository.GetMovementsAsync(cancellationToken);
}
