namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST;

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;
using VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Transform;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class GeofencesController(
    IGeofenceCommandService geofenceCommandService,
    IGeofenceQueryService geofenceQueryService
) : ControllerBase
{
    /// <summary>
    /// Lists all defined geofences, optionally filtered by type (?type=WAREHOUSE|JOB_SITE).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GeofenceResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? type, CancellationToken cancellationToken)
    {
        try
        {
            GeofenceType? parsedType = string.IsNullOrWhiteSpace(type)
                ? null
                : GeofenceAssembler.ParseGeofenceType(type);

            var geofences = await geofenceQueryService.Handle(new GetAllGeofencesQuery(parsedType), cancellationToken);
            var resources = geofences.Select(GeofenceAssembler.ToResourceFromEntity);
            return Ok(resources);
        }
        catch (FleetException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a single geofence by its identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GeofenceResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var geofence = await geofenceQueryService.Handle(new GetGeofenceByIdQuery(id), cancellationToken);
        if (geofence is null)
            return NotFound(new { code = FleetErrorCodes.GeofenceNotFound, message = $"Geofence {id} not found." });

        return Ok(GeofenceAssembler.ToResourceFromEntity(geofence));
    }

    /// <summary>
    /// Defines a new perimeter geofence for a warehouse or construction job site.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GeofenceResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DefineGeofence([FromBody] DefineGeofenceResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = GeofenceAssembler.ToCommandFromResource(resource);
            var geofence = await geofenceCommandService.Handle(command, cancellationToken);
            if (geofence is null) return BadRequest();

            var response = GeofenceAssembler.ToResourceFromEntity(geofence);
            return CreatedAtAction(nameof(GetById), new { id = geofence.Id }, response);
        }
        catch (FleetException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }
}
