namespace VigiaPlatform.API.InventoryManagement.Domain.Repositories;

using VigiaPlatform.API.InventoryManagement.Domain.Model;

public interface IInventoryRepository
{
    Task<IReadOnlyList<Material>> GetMaterialsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryMovement>> GetMovementsAsync(CancellationToken cancellationToken);
}
