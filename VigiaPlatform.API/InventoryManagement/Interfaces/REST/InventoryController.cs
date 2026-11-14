namespace VigiaPlatform.API.InventoryManagement.Interfaces.REST;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.InventoryManagement.Application;

public record MaterialResource(int Id, string Name, string Category, string Unit, decimal CurrentStock,
    decimal MinimumStock, string? ProjectId, string ProjectName);
public record InventoryMovementResource(int Id, int MaterialId, string MaterialName, string? ProjectId,
    string ProjectName, string Type, decimal Quantity, string Unit, string Date);

[ApiController]
[Route("api/v1")]
[Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
public class InventoryController(InventoryQueryService service) : ControllerBase
{
    [HttpGet("materials")]
    public async Task<IActionResult> Materials(CancellationToken cancellationToken) =>
        Ok((await service.GetMaterialsAsync(cancellationToken)).Select(m => new MaterialResource(m.Id, m.Name,
            m.Category, m.Unit, m.CurrentStock, m.MinimumStock, m.ProjectId, m.ProjectName)));

    [HttpGet("inventory-movements")]
    public async Task<IActionResult> Movements(CancellationToken cancellationToken) =>
        Ok((await service.GetMovementsAsync(cancellationToken)).Select(m => new InventoryMovementResource(m.Id,
            m.MaterialId, m.MaterialName, m.ProjectId, m.ProjectName, m.Type, m.Quantity, m.Unit, m.Date.ToString("yyyy-MM-dd"))));
}
