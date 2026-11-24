using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Roles = "ADMIN,SITE_MANAGER")]
[Tags("Orders")]
public class ResidentOrdersController(ResidentOrderService service) : ControllerBase
{
    private int AccountId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("request-catalog")]
    [EndpointSummary("Get assigned projects and requestable materials")]
    [ProducesResponseType<IReadOnlyList<RequestProjectResource>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Catalog(CancellationToken ct) =>
        Ok(await service.CatalogAsync(AccountId, User.IsInRole("ADMIN"), ct));

    [HttpPost("requests")]
    [EndpointSummary("Submit a material request for an assigned project")]
    [ProducesResponseType<OrderResource>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(SubmitMaterialRequestResource input, CancellationToken ct)
    {
        try
        {
            var order = await service.SubmitAsync(AccountId, User.IsInRole("ADMIN"), input, ct);
            return Created($"/api/v1/orders/{order.Id}", OrderResourceFromEntityAssembler.ToResourceFromEntity(order));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException exception) { return BadRequest(new { code = "INVALID_REQUEST", message = exception.Message }); }
        catch (DbUpdateException exception) when (exception.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        { return Conflict(new { code = "REQUEST_CONFLICT", message = "Unable to allocate a request code. Please retry." }); }
    }
}
