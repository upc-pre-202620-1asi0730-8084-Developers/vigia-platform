namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST;

using System.Security.Claims;
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
        var reception = await service.RegisterAsync(input, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.Identity!.Name!, ct);
        return Created($"/api/v1/receptions/{reception.Id}", ReceptionAssembler.ToResourceFromEntity(reception));
    }

    [HttpPost("{id:int}/verify")]
    public async Task<IActionResult> Verify(int id, PhysicalVerification input, CancellationToken ct)
    {
        var reception = await service.VerifyAsync(id, input, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        return reception is null ? NotFound() : Ok(ReceptionAssembler.ToResourceFromEntity(reception));
    }
}
