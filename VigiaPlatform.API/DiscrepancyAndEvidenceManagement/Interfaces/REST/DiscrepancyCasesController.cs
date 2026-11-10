using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Queries;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.Rest.Resources;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.Rest.Transform;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.Rest;

[ApiController]
[Route("api/v1/discrepancy-cases")]
public class DiscrepancyCasesController(
    IDiscrepancyCaseQueryService discrepancyCaseQueryService)
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

        var discrepancies = await discrepancyCaseQueryService.Handle(
            query, cancellationToken);

        var resources = discrepancies
            .Select(
                DiscrepancyCaseResourceFromEntityAssembler
                    .ToResourceFromEntity)
            .ToList();

        return Ok(resources);
    }
}