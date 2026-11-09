namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST;

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.CommandServices;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Queries;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.QueryServices;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Resources;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Transform;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class ReceptionsController(
    IReceptionCommandService receptionCommandService,
    IReceptionQueryService receptionQueryService
) : ControllerBase
{
    /// <summary>
    /// Executes EventStorming Command: "Record unit arrival".
    /// </summary>
    [HttpPost("arrivals")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordUnitArrival([FromBody] RecordUnitArrivalResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ReceptionAssembler.ToCommandFromResource(resource);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null) return BadRequest();
            var response = ReceptionAssembler.ToResourceFromEntity(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
        }
        catch (SiteReceptionException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    /// <summary>
    /// Returns registered arrivals awaiting confirmation (tablero de próximas llegadas en obra).
    /// </summary>
    [HttpGet("upcoming-arrivals")]
    [ProducesResponseType(typeof(IEnumerable<ReceptionResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingArrivals(CancellationToken cancellationToken)
    {
        var result = await receptionQueryService.Handle(new GetUpcomingArrivalsQuery(), cancellationToken);
        var response = result.Select(ReceptionAssembler.ToResourceFromEntity);
        return Ok(response);
    }

    /// <summary>
    /// Lists all receptions, optionally filtered by site ID.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReceptionResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? siteId, CancellationToken cancellationToken)
    {
        var result = siteId.HasValue
            ? await receptionQueryService.Handle(new GetReceptionsBySiteIdQuery(siteId.Value), cancellationToken)
            : await receptionQueryService.Handle(new GetAllReceptionsQuery(), cancellationToken);

        var response = result.Select(ReceptionAssembler.ToResourceFromEntity);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a single reception by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await receptionQueryService.Handle(new GetReceptionByIdQuery(id), cancellationToken);
        if (result is null) return NotFound(new { message = $"Reception {id} not found." });
        return Ok(ReceptionAssembler.ToResourceFromEntity(result));
    }

    /// <summary>
    /// Retrieves a single reception by its dispatch ID reference.
    /// </summary>
    [HttpGet("by-dispatch/{dispatchId}")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDispatchId(string dispatchId, CancellationToken cancellationToken)
    {
        var result = await receptionQueryService.Handle(new GetReceptionByDispatchIdQuery(dispatchId), cancellationToken);
        if (result is null) return NotFound(new { message = $"Reception for dispatch '{dispatchId}' not found." });
        return Ok(ReceptionAssembler.ToResourceFromEntity(result));
    }

    /// <summary>
    /// Executes EventStorming Command: "Confirm Receipt".
    /// </summary>
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmReceipt(int id, [FromBody] ConfirmReceiptResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ReceptionAssembler.ToCommandFromResource(id, resource);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null) return NotFound();
            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Verify received material".
    /// </summary>
    [HttpPost("{id:int}/verify-material")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyMaterial(int id, [FromBody] VerifyMaterialResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new VerifyReceivedMaterialCommand(id, resource.MaterialId, resource.ReceivedQuantity, resource.Comments);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null) return NotFound();
            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Registrar conformidad" (Compliance recorded -> Office closed).
    /// </summary>
    [HttpPost("{id:int}/conformity")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordConformity(int id, [FromBody] RecordConformityResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RecordConformityCommand(id, resource.ComplianceNotes);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null) return NotFound();
            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Report discrepancy".
    /// </summary>
    [HttpPost("{id:int}/discrepancy")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportDiscrepancy(int id, [FromBody] ReportDiscrepancyResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ReportDiscrepancyCommand(id, resource.Reason, resource.Description, resource.Severity);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null) return NotFound();
            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Attach photographic evidence".
    /// </summary>
    [HttpPost("{id:int}/evidence")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachEvidence(int id, [FromBody] AttachEvidenceResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new AttachPhotographicEvidenceCommand(id, resource.EvidenceType, resource.FileUrl, resource.Description);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null) return NotFound();
            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }
}
