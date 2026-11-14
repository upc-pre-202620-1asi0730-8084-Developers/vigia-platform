using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Queries;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;
using VigiaPlatform.API.TransitTraceability.Domain.Services;
using VigiaPlatform.API.TransitTraceability.Interfaces.REST.Resources;
using VigiaPlatform.API.TransitTraceability.Interfaces.REST.Transform;

namespace VigiaPlatform.API.TransitTraceability.Interfaces.REST.Controllers;

/// <summary>
/// Route tracking endpoints of the Transit Traceability bounded context (BC-06).
/// </summary>
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER,SITE_MANAGER")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class RoutesController(IRouteCommandService commandService, IRouteQueryService queryService) : ControllerBase
{
    /// <summary>Lists the tracked routes, optionally filtered by status (IN_TRANSIT, DEVIATED, STOPPED, SIGNAL_LOST, ARRIVED).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RouteResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllRoutes([FromQuery] string? status, CancellationToken cancellationToken)
    {
        RouteStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!EnumContract.TryParseContract<RouteStatus>(status, out var parsed))
                return BadRequest(new ErrorResource("INVALID_STATUS", $"Unknown route status: {status}."));
            filter = parsed;
        }

        var routes = await queryService.Handle(new GetAllRoutesQuery(filter), cancellationToken);
        return Ok(routes.Select(RouteResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Gets a route with its planned path, telemetry and alerts.</summary>
    [HttpGet("{routeId:int}")]
    [ProducesResponseType(typeof(RouteResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRouteById(int routeId, CancellationToken cancellationToken)
    {
        var route = await queryService.Handle(new GetRouteByIdQuery(routeId), cancellationToken);
        return route is null
            ? NotFound(new ErrorResource(TransitTraceabilityErrorCodes.RouteNotFound, $"Route {routeId} not found."))
            : Ok(RouteResourceFromEntityAssembler.ToResourceFromEntity(route));
    }

    /// <summary>Gets the alerts raised by a route.</summary>
    [HttpGet("{routeId:int}/alerts")]
    [ProducesResponseType(typeof(IEnumerable<TransitAlertResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRouteAlerts(int routeId, CancellationToken cancellationToken)
    {
        var route = await queryService.Handle(new GetRouteByIdQuery(routeId), cancellationToken);
        return route is null
            ? NotFound(new ErrorResource(TransitTraceabilityErrorCodes.RouteNotFound, $"Route {routeId} not found."))
            : Ok(route.Alerts.Select(TransitAlertResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Starts tracking the route of a dispatch.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RouteResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteResource resource, CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(CreateRouteCommandFromResourceAssembler.ToCommandFromResource(resource), cancellationToken);
        return result.Fold<IActionResult>(
            route => CreatedAtAction(nameof(GetRouteById), new { routeId = route.Id },
                RouteResourceFromEntityAssembler.ToResourceFromEntity(route)),
            ToErrorResult);
    }

    /// <summary>Registers a GPS position reported by the telemetry platform and returns the alerts it raised.</summary>
    [HttpPost("{routeId:int}/positions")]
    [ProducesResponseType(typeof(TransitOutcomeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterPosition(int routeId, [FromBody] RegisterPositionResource resource, CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(RegisterPositionCommandFromResourceAssembler.ToCommandFromResource(routeId, resource), cancellationToken);
        return result.Fold<IActionResult>(
            outcome => Ok(TransitOutcomeResourceFromEntityAssembler.ToResourceFromOutcome(outcome)),
            ToErrorResult);
    }

    /// <summary>Registers that the GPS device of the route stopped reporting.</summary>
    [HttpPost("{routeId:int}/signal-lost")]
    [ProducesResponseType(typeof(TransitOutcomeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReportSignalLost(int routeId, [FromBody] ReportSignalLostResource? resource, CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(new ReportSignalLostCommand(routeId, resource?.DetectedAt), cancellationToken);
        return result.Fold<IActionResult>(
            outcome => Ok(TransitOutcomeResourceFromEntityAssembler.ToResourceFromOutcome(outcome)),
            ToErrorResult);
    }

    /// <summary>Marks a deviation or prolonged-stop alert as notified to the destination site.</summary>
    [HttpPost("{routeId:int}/alerts/{alertId:int}/notify")]
    [ProducesResponseType(typeof(TransitAlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> NotifyDestinationSite(int routeId, int alertId, CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(new NotifyDestinationSiteCommand(routeId, alertId), cancellationToken);
        return result.Fold<IActionResult>(
            alert => Ok(TransitAlertResourceFromEntityAssembler.ToResourceFromEntity(alert)),
            ToErrorResult);
    }

    private ObjectResult ToErrorResult(TransitTraceabilityError error)
    {
        var body = new ErrorResource(error.Code, error.Message);
        return error.Code switch
        {
            TransitTraceabilityErrorCodes.RouteNotFound or TransitTraceabilityErrorCodes.AlertNotFound => NotFound(body),
            TransitTraceabilityErrorCodes.RouteAlreadyArrived
                or TransitTraceabilityErrorCodes.SignalAlreadyLost
                or TransitTraceabilityErrorCodes.DispatchAlreadyTracked
                or TransitTraceabilityErrorCodes.AlertNotNotifiable => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
