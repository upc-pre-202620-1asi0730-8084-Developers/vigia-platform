using System.Globalization;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest;

[ApiController]
[Route("api/v1/dispatches")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Dispatches")]
public class DispatchesController(
    IDispatchCommandService dispatchCommandService,
    IDispatchQueryService dispatchQueryService) : ControllerBase
{
    /// <summary>
    ///     Gets all dispatches, optionally filtered by status, departure date or related order.
    /// </summary>
    [HttpGet]
    [EndpointSummary("Get all dispatches")]
    [EndpointDescription("Get all dispatches. Optional filters: status, departureDate (yyyy-MM-dd), relatedOrderId.")]
    [ProducesResponseType(typeof(IEnumerable<DispatchResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDispatches(
        [FromQuery] string? status,
        [FromQuery] string? departureDate,
        [FromQuery] string? relatedOrderId,
        CancellationToken cancellationToken)
    {
        IEnumerable<Dispatch> dispatches;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DispatchStatus>(status.Replace("_", string.Empty), true, out var parsedStatus))
                return BadRequest($"Invalid status '{status}'");

            dispatches = await dispatchQueryService.Handle(new GetDispatchesByStatusQuery(parsedStatus),
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(departureDate))
        {
            if (!DateOnly.TryParseExact(departureDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate))
                return BadRequest("Departure date must have the format yyyy-MM-dd");

            dispatches = await dispatchQueryService.Handle(new GetDispatchesByDepartureDateQuery(parsedDate),
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(relatedOrderId))
        {
            dispatches = await dispatchQueryService.Handle(new GetDispatchesByRelatedOrderQuery(relatedOrderId),
                cancellationToken);
        }
        else
        {
            dispatches = await dispatchQueryService.Handle(new GetAllDispatchesQuery(), cancellationToken);
        }

        return Ok(dispatches.Select(DispatchResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets a dispatch by id.
    /// </summary>
    [HttpGet("{dispatchId:int}")]
    [EndpointSummary("Get a dispatch by id")]
    [EndpointDescription("Get a dispatch with its items and timeline by its unique identifier.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDispatchById(int dispatchId, CancellationToken cancellationToken)
    {
        var dispatch = await dispatchQueryService.Handle(new GetDispatchByIdQuery(dispatchId), cancellationToken);

        return DispatchActionResultAssembler.ToActionResultFromGetDispatchByIdResult(
            this,
            dispatch,
            found => Ok(DispatchResourceFromEntityAssembler.ToResourceFromEntity(found)));
    }

    /// <summary>
    ///     Creates a dispatch.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Create a dispatch")]
    [EndpointDescription("Create a new dispatch with its material items. It starts with the SCHEDULED status.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDispatch([FromBody] CreateDispatchResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = CreateDispatchCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await dispatchCommandService.Handle(command, cancellationToken);

            return DispatchActionResultAssembler.ToActionResultFromDispatchResult(
                this,
                result,
                created => CreatedAtAction(nameof(GetDispatchById), new { dispatchId = created.Id },
                    DispatchResourceFromEntityAssembler.ToResourceFromEntity(created)));
        }
        catch (ArgumentException exception)
        {
            return DispatchActionResultAssembler.ToBadRequest(this, exception);
        }
    }

    /// <summary>
    ///     Adds an item to a dispatch.
    /// </summary>
    [HttpPost("{dispatchId:int}/items")]
    [EndpointSummary("Add an item to a dispatch")]
    [EndpointDescription("Add a material item to a dispatch that has not left the warehouse yet.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddDispatchItem(int dispatchId, [FromBody] AddDispatchItemResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = DispatchCommandFromResourceAssembler.ToCommandFromResource(dispatchId, resource);
            var result = await dispatchCommandService.Handle(command, cancellationToken);

            return DispatchActionResultAssembler.ToActionResultFromDispatchResult(
                this,
                result,
                updated => Ok(DispatchResourceFromEntityAssembler.ToResourceFromEntity(updated)));
        }
        catch (ArgumentException exception)
        {
            return DispatchActionResultAssembler.ToBadRequest(this, exception);
        }
    }

    /// <summary>
    ///     Removes an item from a dispatch.
    /// </summary>
    [HttpDelete("{dispatchId:int}/items/{itemId:int}")]
    [EndpointSummary("Remove an item from a dispatch")]
    [EndpointDescription("Remove a material item from a dispatch. A dispatch must keep at least one item.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveDispatchItem(int dispatchId, int itemId,
        [FromBody] RemoveDispatchItemResource resource, CancellationToken cancellationToken)
    {
        var command = DispatchCommandFromResourceAssembler.ToCommandFromResource(dispatchId, itemId, resource);
        var result = await dispatchCommandService.Handle(command, cancellationToken);

        return DispatchActionResultAssembler.ToActionResultFromDispatchResult(
            this,
            result,
            updated => Ok(DispatchResourceFromEntityAssembler.ToResourceFromEntity(updated)));
    }

    /// <summary>
    ///     Assigns a vehicle to a dispatch.
    /// </summary>
    [HttpPut("{dispatchId:int}/vehicle")]
    [EndpointSummary("Assign a vehicle to a dispatch")]
    [EndpointDescription("Assign the vehicle that will transport the dispatch.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignVehicle(int dispatchId, [FromBody] AssignVehicleResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = DispatchCommandFromResourceAssembler.ToCommandFromResource(dispatchId, resource);
            var result = await dispatchCommandService.Handle(command, cancellationToken);

            return DispatchActionResultAssembler.ToActionResultFromDispatchResult(
                this,
                result,
                updated => Ok(DispatchResourceFromEntityAssembler.ToResourceFromEntity(updated)));
        }
        catch (ArgumentException exception)
        {
            return DispatchActionResultAssembler.ToBadRequest(this, exception);
        }
    }

    /// <summary>
    ///     Changes the status of a dispatch.
    /// </summary>
    [HttpPatch("{dispatchId:int}/status")]
    [EndpointSummary("Change the status of a dispatch")]
    [EndpointDescription("Move the dispatch to the next status: IN_PREPARATION, IN_TRANSIT or COMPLETED.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDispatchStatus(int dispatchId,
        [FromBody] ChangeDispatchStatusResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = DispatchCommandFromResourceAssembler.ToCommandFromResource(dispatchId, resource);
            var result = await dispatchCommandService.Handle(command, cancellationToken);

            return DispatchActionResultAssembler.ToActionResultFromDispatchResult(
                this,
                result,
                updated => Ok(DispatchResourceFromEntityAssembler.ToResourceFromEntity(updated)));
        }
        catch (ArgumentException exception)
        {
            return DispatchActionResultAssembler.ToBadRequest(this, exception);
        }
    }

    /// <summary>
    ///     Cancels a dispatch.
    /// </summary>
    [HttpPost("{dispatchId:int}/cancellation")]
    [EndpointSummary("Cancel a dispatch")]
    [EndpointDescription("Cancel a dispatch that has not left the warehouse yet.")]
    [ProducesResponseType(typeof(DispatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelDispatch(int dispatchId, [FromBody] CancelDispatchResource resource,
        CancellationToken cancellationToken)
    {
        var command = DispatchCommandFromResourceAssembler.ToCommandFromResource(dispatchId, resource);
        var result = await dispatchCommandService.Handle(command, cancellationToken);

        return DispatchActionResultAssembler.ToActionResultFromDispatchResult(
            this,
            result,
            updated => Ok(DispatchResourceFromEntityAssembler.ToResourceFromEntity(updated)));
    }
}
