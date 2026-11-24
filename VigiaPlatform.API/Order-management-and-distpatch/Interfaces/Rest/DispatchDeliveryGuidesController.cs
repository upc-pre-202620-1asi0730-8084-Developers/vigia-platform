using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest;

[ApiController]
[Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
[Route("api/v1/dispatches/{dispatchId:int}/delivery-guide")]
[Tags("Dispatches")]
public class DispatchDeliveryGuidesController(DispatchDeliveryGuideService service) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Issue a delivery guide and authorize departure")]
    [EndpointDescription("Requires IN_PREPARATION, assigned vehicle and a persisted current net weighing within tolerance. Saves an immutable load/vehicle/weighing snapshot and server actor/time. An optional requestId permits safe replay with identical driver/carrier details. Issuing a guide locks load, vehicle and weighing; departure still requires the existing status action.")]
    [ProducesResponseType<DispatchDeliveryGuideResource>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(int dispatchId, IssueDispatchDeliveryGuideResource input, CancellationToken ct)
    {
        try
        {
            var dispatch = await service.IssueAsync(dispatchId, input.DriverName, input.CarrierName, input.RequestId,
                int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!, ct);
            return CreatedAtAction(nameof(Find), new { dispatchId }, DispatchDeliveryGuideResourceAssembler.ToResource(dispatch));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { code = "INVALID_GUIDE", message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { code = "GUIDE_CONFLICT", message = exception.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { code = "DISPATCH_CHANGED", message = "The dispatch changed. Reload before issuing a guide." }); }
        catch (DbUpdateException exception) when (exception.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1205 or 1213 })
        { return Conflict(new { code = "DISPATCH_CHANGED", message = "Another operation changed the dispatch. Retry using the same request ID." }); }
        catch (DbUpdateException exception) when (exception.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        { return Conflict(new { code = "GUIDE_EXISTS", message = "A guide was already issued. Reload or replay the same request." }); }
    }

    [HttpGet]
    [EndpointSummary("Read the persisted delivery guide")]
    [ProducesResponseType<DispatchDeliveryGuideResource>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Find(int dispatchId, CancellationToken ct)
    {
        try
        {
            var dispatch = await service.FindAsync(dispatchId, ct);
            var guide = DispatchDeliveryGuideResourceAssembler.ToResource(dispatch);
            return guide is null ? NotFound() : Ok(guide);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
