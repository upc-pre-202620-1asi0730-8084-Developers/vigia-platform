namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.TransitTraceability.Domain.Exceptions;
using VigiaPlatform.API.TransitTraceability.Infrastructure.Integration;
public record TelemetryReport(string GpsDeviceId,
    [property:System.Text.Json.Serialization.JsonRequired] double Latitude,
    [property:System.Text.Json.Serialization.JsonRequired] double Longitude,
    DateTimeOffset? Timestamp, double? SpeedKmh=null);
[ApiController,Route("api/v1/telemetry/reports"),Authorize(Roles="ADMIN,WAREHOUSE_MANAGER")]
public class TelemetryController(DispatchTransitService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Report(TelemetryReport input,CancellationToken ct)
    {
        try { return Ok(await service.ReportAsync(input.GpsDeviceId,input.Latitude,input.Longitude,input.Timestamp,input.SpeedKmh,User.FindFirstValue("company_id")??"",ct)); }
        catch(TripException ex) { return StatusCode(ex.Status,new {code=ex.Code,message=ex.Message}); }
        catch(FleetException ex) { return BadRequest(new {code=ex.Code,message=ex.Message}); }
        catch(TransitTraceabilityException ex) { return Conflict(new {code=ex.Code,message=ex.Message}); }
        catch(DbUpdateConcurrencyException) { return Conflict(new {code="CONCURRENT_TRIP_CHANGE",message="Refresh and retry the report."}); }
        catch(DbUpdateException ex) when(ex.GetBaseException() is MySqlException {Number:1062 or 1205 or 1213})
        { return Conflict(new {code="CONCURRENT_TRIP_CHANGE",message="A concurrent report or trip change prevented this write."}); }
    }
}
