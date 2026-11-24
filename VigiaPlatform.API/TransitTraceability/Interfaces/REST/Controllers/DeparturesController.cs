namespace VigiaPlatform.API.TransitTraceability.Interfaces.REST.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VigiaPlatform.API.TransitTraceability.Infrastructure.Integration;
using VigiaPlatform.API.TransitTraceability.Interfaces.REST.Resources;
using VigiaPlatform.API.TransitTraceability.Interfaces.REST.Transform;

[ApiController, Authorize(Roles="ADMIN,WAREHOUSE_MANAGER"), Tags("Dispatches")]
[Route("api/v1/dispatches/{dispatchId:int}/departure")]
public class DeparturesController(DispatchTransitService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Options(int dispatchId,CancellationToken ct)
    {
        try { return Ok(await service.OptionsAsync(dispatchId,User.FindFirstValue("company_id")??"",ct)); }
        catch(TripException ex) { return StatusCode(ex.Status,new {code=ex.Code,message=ex.Message}); }
    }
    [HttpPost]
    [ProducesResponseType(typeof(RouteResource),StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Depart(int dispatchId,DeparturePlan input,CancellationToken ct)
    {
        try
        {
            var route=await service.DepartAsync(dispatchId,input,User.Identity?.Name??"",User.FindFirstValue("company_id")??"",ct);
            return Created($"/api/v1/routes/{route.Id}",RouteResourceFromEntityAssembler.ToResourceFromEntity(route));
        }
        catch(TripException ex) { return StatusCode(ex.Status,new {code=ex.Code,message=ex.Message}); }
        catch(Domain.Exceptions.TransitTraceabilityException ex) { return BadRequest(new {code=ex.Code,message=ex.Message}); }
        catch(DbUpdateConcurrencyException) { return Conflict(new {code="CONCURRENT_TRIP_CHANGE",message="Refresh and retry this trip operation."}); }
        catch(DbUpdateException ex) when(ex.GetBaseException() is MySqlException {Number:1062 or 1205 or 1213})
        { return Conflict(new {code="CONCURRENT_TRIP_CHANGE",message="A trip or vehicle was reserved concurrently; refresh before retrying."}); }
    }
}
