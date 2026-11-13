namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST;

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;
using VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Transform;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class VehiclesController(
    IVehicleCommandService vehicleCommandService,
    IVehicleQueryService vehicleQueryService
) : ControllerBase
{
    /// <summary>
    /// Lists all registered vehicles, optionally filtered by GPS pairing status (?isGpsPaired=true|false).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VehicleResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isGpsPaired, CancellationToken cancellationToken)
    {
        var vehicles = await vehicleQueryService.Handle(new GetAllVehiclesQuery(isGpsPaired), cancellationToken);
        var resources = vehicles.Select(VehicleAssembler.ToResourceFromEntity);
        return Ok(resources);
    }

    /// <summary>
    /// Retrieves a single vehicle by its identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleQueryService.Handle(new GetVehicleByIdQuery(id), cancellationToken);
        if (vehicle is null)
            return NotFound(new { code = FleetErrorCodes.VehicleNotFound, message = $"Vehicle {id} not found." });

        return Ok(VehicleAssembler.ToResourceFromEntity(vehicle));
    }

    /// <summary>
    /// Registers a new transport unit in the fleet registry.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterVehicle([FromBody] RegisterVehicleResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = VehicleAssembler.ToCommandFromResource(resource);
            var vehicle = await vehicleCommandService.Handle(command, cancellationToken);
            if (vehicle is null) return BadRequest();

            var response = VehicleAssembler.ToResourceFromEntity(vehicle);
            return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, response);
        }
        catch (FleetException ex)
        {
            return MapFleetException(ex);
        }
    }

    /// <summary>
    /// Links an available telematics GPS device to a transport unit (1:1 pairing).
    /// </summary>
    [HttpPost("{id:int}/gps-device")]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PairGpsDevice(int id, [FromBody] PairGpsDeviceResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = VehicleAssembler.ToCommandFromResource(id, resource);
            var vehicle = await vehicleCommandService.Handle(command, cancellationToken);
            if (vehicle is null) return NotFound();

            return Ok(VehicleAssembler.ToResourceFromEntity(vehicle));
        }
        catch (FleetException ex)
        {
            return MapFleetException(ex);
        }
    }

    /// <summary>
    /// Updates the real-time WGS84 telemetry position of a vehicle.
    /// </summary>
    [HttpPost("{id:int}/position")]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdateVehiclePositionResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = VehicleAssembler.ToCommandFromResource(id, resource);
            var vehicle = await vehicleCommandService.Handle(command, cancellationToken);
            if (vehicle is null) return NotFound();

            return Ok(VehicleAssembler.ToResourceFromEntity(vehicle));
        }
        catch (FleetException ex)
        {
            return MapFleetException(ex);
        }
    }

    private IActionResult MapFleetException(FleetException ex) => ex.Code switch
    {
        FleetErrorCodes.VehicleNotFound => NotFound(new { code = ex.Code, message = ex.Message }),
        FleetErrorCodes.DuplicateLicensePlate or FleetErrorCodes.DeviceAlreadyAssigned =>
            Conflict(new { code = ex.Code, message = ex.Message }),
        _ => BadRequest(new { code = ex.Code, message = ex.Message })
    };
}
