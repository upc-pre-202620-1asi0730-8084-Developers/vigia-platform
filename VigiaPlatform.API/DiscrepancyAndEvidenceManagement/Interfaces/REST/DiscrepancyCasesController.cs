using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Queries;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Transform;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST;

[ApiController]
[Route("api/v1/discrepancy-cases")]
public class DiscrepancyCasesController(
    IDiscrepancyCaseCommandService commandService,
    IDiscrepancyCaseQueryService queryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IEnumerable<DiscrepancyCaseResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDiscrepancyCases(
        CancellationToken cancellationToken)
    {
        var query = new GetAllDiscrepancyCasesQuery();

        var discrepancies = await queryService.Handle(
            query, cancellationToken);

        var resources = discrepancies
            .Select(
                DiscrepancyCaseResourceFromEntityAssembler
                    .ToResourceFromEntity)
            .ToList();

        return Ok(resources);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(DiscrepancyCaseResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscrepancyCaseById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetDiscrepancyCaseByIdQuery(id);

        var discrepancyCase = await queryService.Handle(
            query, cancellationToken);

        if (discrepancyCase is null)
        {
            return NotFound();
        }

        var resource =
            DiscrepancyCaseResourceFromEntityAssembler
                .ToResourceFromEntity(discrepancyCase);

        return Ok(resource);
    }

    [HttpGet("{id:int}/timeline")]
    [ProducesResponseType(
        typeof(IEnumerable<DiscrepancyCaseTimelineEventResource>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscrepancyCaseTimeline(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetDiscrepancyCaseTimelineQuery(id);

        var discrepancyCase = await queryService.Handle(
            query, cancellationToken);

        if (discrepancyCase is null)
        {
            return NotFound();
        }

        var resources = discrepancyCase.Events
            .OrderBy(discrepancyEvent => discrepancyEvent.OccurredAt)
            .ThenBy(discrepancyEvent => discrepancyEvent.Id)
            .Select(
                DiscrepancyCaseTimelineEventResourceFromEntityAssembler
                    .ToResourceFromEntity)
            .ToList();

        return Ok(resources);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(DiscrepancyCaseResource),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDiscrepancyCase(
        [FromBody] CreateDiscrepancyCaseResource resource,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resource.DispatchId) ||
            string.IsNullOrWhiteSpace(resource.ReceptionId) ||
            string.IsNullOrWhiteSpace(resource.CaseNumber))
        {
            return BadRequest("Invalid discrepancy case data.");
        }

        var command =
            CreateDiscrepancyCaseCommandFromResourceAssembler
                .ToCommandFromResource(resource);

        var discrepancyCase = await commandService.Handle(
            command, cancellationToken);

        var createdResource =
            DiscrepancyCaseResourceFromEntityAssembler
                .ToResourceFromEntity(discrepancyCase);

        return StatusCode(
            StatusCodes.Status201Created,
            createdResource);
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(
        typeof(DiscrepancyCaseResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDiscrepancyCase(
        [FromRoute] int id,
        [FromBody] DetermineDiscrepancyResponsibilityResource resource,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid discrepancy case ID.");
        }

        bool hasResponsibility = resource.Responsibility is not null;
        bool hasStatus = resource.Status is not null;

        if (hasResponsibility == hasStatus)
        {
            return BadRequest(
                "Provide either responsibility or status.");
        }

        try
        {
            if (hasResponsibility)
            {
                if (resource.ResolutionSummary is not null)
                {
                    return BadRequest(
                        "Resolution summary is only allowed when closing.");
                }

                if (resource.Responsibility != "Warehouse" &&
                    resource.Responsibility != "Transport" &&
                    resource.Responsibility != "Site")
                {
                    return BadRequest(
                        "Responsibility must be Warehouse, Transport or Site.");
                }

                var command =
                    DetermineDiscrepancyResponsibilityCommandFromResourceAssembler
                        .ToCommandFromResource(id, resource);

                var discrepancyCase = await commandService.Handle(
                    command,
                    cancellationToken);

                if (discrepancyCase is null)
                {
                    return NotFound();
                }

                return Ok(
                    DiscrepancyCaseResourceFromEntityAssembler
                        .ToResourceFromEntity(discrepancyCase));
            }

            if (resource.Status != "Closed")
            {
                return BadRequest(
                    "Only the Closed status is supported.");
            }

            var closeCommand = new CloseDiscrepancyCaseCommand(
                id,
                resource.ResolutionSummary);

            var closedCase = await commandService.Handle(
                closeCommand,
                cancellationToken);

            if (closedCase is null)
            {
                return NotFound();
            }

            return Ok(
                DiscrepancyCaseResourceFromEntityAssembler
                    .ToResourceFromEntity(closedCase));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
