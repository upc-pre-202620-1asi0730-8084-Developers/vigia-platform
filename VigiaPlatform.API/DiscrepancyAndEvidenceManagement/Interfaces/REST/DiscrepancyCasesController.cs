using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
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
}
