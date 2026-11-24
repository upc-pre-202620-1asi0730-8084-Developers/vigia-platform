using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest;

[ApiController]
[Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
[Route("api/v1/dispatches/{dispatchId:int}/weighing")]
[Tags("Dispatches")]
public class DispatchWeighingsController(DispatchWeighingService service) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Record net cargo weight in kg")]
    [EndpointDescription("Requires an IN_PREPARATION dispatch and assigned vehicle. Supply both expectedWeightKg and toleranceKg to evaluate the difference. Without them, the measurement is saved but requires review. An optional requestId makes retries idempotent.")]
    [ProducesResponseType<DispatchWeighingResource>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Record(int dispatchId, RecordDispatchWeighingResource input, CancellationToken ct)
    {
        try
        {
            var result = await service.RecordAsync(new RecordDispatchWeighingCommand(dispatchId, input.WeightKg,
                input.ExpectedWeightKg, input.ToleranceKg, input.RequestId,
                int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!), ct);
            return CreatedAtAction(nameof(Find), new { dispatchId, weighingId = result.Weighing.Id },
                DispatchWeighingResourceAssembler.ToResource(result.Dispatch, result.Weighing));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { code = "INVALID_WEIGHING", message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { code = "WEIGHING_CONFLICT", message = exception.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { code = "DISPATCH_CHANGED", message = "The dispatch changed. Reload it before recording another weighing." }); }
        catch (DbUpdateException exception) when (exception.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        { return Conflict(new { code = "WEIGHING_CONFLICT", message = "The request was already recorded. Retry using the same request ID." }); }
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DispatchWeighingResource>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History(int dispatchId, CancellationToken ct)
    {
        try
        {
            var dispatch = await service.FindAsync(dispatchId, ct);
            return Ok(dispatch.Weighings.OrderByDescending(w => w.Id).Select(w => DispatchWeighingResourceAssembler.ToResource(dispatch, w)));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("{weighingId:int}")]
    [ProducesResponseType<DispatchWeighingResource>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Find(int dispatchId, int weighingId, CancellationToken ct)
    {
        try
        {
            var dispatch = await service.FindAsync(dispatchId, ct);
            var weighing = dispatch.Weighings.SingleOrDefault(w => w.Id == weighingId);
            return weighing is null ? NotFound() : Ok(DispatchWeighingResourceAssembler.ToResource(dispatch, weighing));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
