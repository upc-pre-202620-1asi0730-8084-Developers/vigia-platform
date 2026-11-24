namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Transform;

[ApiController]
[Route("api/v1/receptions")]
[Authorize(Roles = "ADMIN,SITE_MANAGER")]
public class PhysicalReceptionsController(PhysicalReceptionService service) : ControllerBase
{
    [HttpPost("physical")]
    public async Task<IActionResult> Register(PhysicalArrival input, CancellationToken ct)
    {
        var (reception, replayed) = await service.RegisterAsync(input, User, ct);
        var resource = ReceptionAssembler.ToResourceFromEntity(reception);
        return replayed ? Ok(resource) : Created($"/api/v1/receptions/{reception.Id}", resource);
    }

    [HttpPost("{id:int}/verify")]
    public async Task<IActionResult> Verify(int id, PhysicalVerification input, CancellationToken ct)
    {
        var reception = await service.VerifyAsync(id, input, User, ct);
        return reception is null ? NotFound() : Ok(ReceptionAssembler.ToResourceFromEntity(reception));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PhysicalArrival input, CancellationToken ct)
    {
        var reception = await service.UpdateAsync(id, input, User, ct);
        return reception is null ? NotFound() : Ok(ReceptionAssembler.ToResourceFromEntity(reception));
    }

    [HttpPost("{id:int}/compare")]
    public async Task<IActionResult> Compare(int id, PhysicalComparison input, CancellationToken ct)
    {
        var reception = await service.CompareAsync(id, input, User, ct);
        return reception is null ? NotFound() : Ok(new {
            isConformant = !reception.HasAnyDiscrepancy, hasDiscrepancy = reception.HasAnyDiscrepancy,
            items = reception.Items.Select(ReceptionAssembler.ToItemResource), reception = ReceptionAssembler.ToResourceFromEntity(reception)
        });
    }
}
