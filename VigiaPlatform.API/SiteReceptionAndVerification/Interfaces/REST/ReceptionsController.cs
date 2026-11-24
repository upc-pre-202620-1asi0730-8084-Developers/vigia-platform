namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST;

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.CommandServices;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Queries;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.QueryServices;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;
using VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Resources;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Transform;

/// <summary>
/// REST API endpoints for the Site Reception and Verification Bounded Context (BC-07).
/// Implements the on-site verification flow executed by the Site Resident actor.
/// </summary>
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "ADMIN,SITE_MANAGER")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class ReceptionsController(
    IReceptionCommandService receptionCommandService,
    IReceptionQueryService receptionQueryService,
    EvidenceAccess evidenceAccess,
    PhysicalReceptionService physicalReceptionService,
    AppDbContext db,
    IUnitOfWork unitOfWork
) : ControllerBase
{
    /// <summary>
    /// Executes EventStorming Command: "Record unit arrival".
    /// </summary>
    [HttpPost]
    [HttpPost("arrivals")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordUnitArrival([FromBody] RecordUnitArrivalResource resource, CancellationToken cancellationToken)
    {
        if (!await evidenceAccess.ProjectAsync(resource.SiteId, User, cancellationToken))
            return Forbid();

        if (int.TryParse(resource.DispatchId?.Replace("DS-", "", StringComparison.OrdinalIgnoreCase), out var dispatchId))
        {
            var dispatch = await db.Set<Dispatch>().AsNoTracking().SingleOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);
            if (dispatch is not null && dispatch.SiteId.Value != resource.SiteId.ToString())
                return BadRequest(new ErrorResource(SiteReceptionErrorCodes.InvalidArrival, "Dispatch does not belong to the specified site."));
        }

        try
        {
            var command = ReceptionAssembler.ToCommandFromResource(resource);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null)
                return BadRequest(new ErrorResource(SiteReceptionErrorCodes.InvalidArrival, "Failed to record unit arrival."));

            var response = ReceptionAssembler.ToResourceFromEntity(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
        }
        catch (SiteReceptionException ex)
        {
            return ToErrorResult(ex);
        }
    }

    /// <summary>
    /// Returns registered arrivals awaiting confirmation (tablero de próximas llegadas en obra).
    /// </summary>
    [HttpGet("upcoming-arrivals")]
    [ProducesResponseType(typeof(IEnumerable<ReceptionResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingArrivals(CancellationToken cancellationToken)
    {
        var allowedSites = await evidenceAccess.AccessibleProjectIdsAsync(User, cancellationToken);
        var result = await receptionQueryService.Handle(new GetUpcomingArrivalsQuery(), cancellationToken);
        if (allowedSites is not null)
            result = result.Where(r => allowedSites.Contains(r.SiteId));
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
        if (siteId.HasValue && !await evidenceAccess.ProjectAsync(siteId.Value, User, cancellationToken))
            return Forbid();

        var result = siteId.HasValue
            ? await receptionQueryService.Handle(new GetReceptionsBySiteIdQuery(siteId.Value), cancellationToken)
            : await receptionQueryService.Handle(new GetAllReceptionsQuery(), cancellationToken);

        var allowedSites = await evidenceAccess.AccessibleProjectIdsAsync(User, cancellationToken);
        if (allowedSites is not null)
            result = result.Where(r => allowedSites.Contains(r.SiteId));

        var response = result.Select(ReceptionAssembler.ToResourceFromEntity);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a single reception by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!await evidenceAccess.ReceptionAsync(id, User, cancellationToken))
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        var result = await receptionQueryService.Handle(new GetReceptionByIdQuery(id), cancellationToken);
        if (result is null)
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        return Ok(ReceptionAssembler.ToResourceFromEntity(result));
    }

    /// <summary>
    /// Retrieves a single reception by its dispatch ID reference.
    /// </summary>
    [HttpGet("by-dispatch/{dispatchId}")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDispatchId(string dispatchId, CancellationToken cancellationToken)
    {
        var result = await receptionQueryService.Handle(new GetReceptionByDispatchIdQuery(dispatchId), cancellationToken);
        if (result is null || !await evidenceAccess.ReceptionAsync(result.Id, User, cancellationToken))
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception for dispatch '{dispatchId}' not found."));

        return Ok(ReceptionAssembler.ToResourceFromEntity(result));
    }

    /// <summary>
    /// Executes EventStorming Command: "Confirm Receipt".
    /// </summary>
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmReceipt(int id, [FromBody] ConfirmReceiptResource resource, CancellationToken cancellationToken)
    {
        if (!await evidenceAccess.ReceptionAsync(id, User, cancellationToken))
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        var existing = await receptionQueryService.Handle(new GetReceptionByIdQuery(id), cancellationToken);
        if (existing is null)
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        if (int.TryParse(existing.DispatchId?.Replace("DS-", "", StringComparison.OrdinalIgnoreCase), out var dispatchId))
        {
            var dispatch = await db.Set<Dispatch>().Include(d => d.DeliveryGuide).AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);
            if (dispatch?.DeliveryGuide is not null &&
                !string.Equals(resource.DeliveryNoteNumber?.Trim(), dispatch.DeliveryGuide.GuideNumber, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ErrorResource(SiteReceptionErrorCodes.InvalidArrival,
                    "Delivery note number does not match the dispatch delivery guide."));
            }
        }

        try
        {
            var command = ReceptionAssembler.ToCommandFromResource(id, resource);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null)
                return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return ToErrorResult(ex);
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Verify received material".
    /// </summary>
    [HttpPost("{id:int}/verify-material")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyMaterial(int id, [FromBody] VerifyMaterialResource resource, CancellationToken cancellationToken)
    {
        if (!await evidenceAccess.ReceptionAsync(id, User, cancellationToken))
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        try
        {
            var command = new VerifyReceivedMaterialCommand(id, resource.MaterialId, resource.ReceivedQuantity, resource.Comments);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null)
                return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return ToErrorResult(ex);
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Registrar conformidad" (Compliance recorded -> Office closed).
    /// </summary>
    [HttpPost("{id:int}/conformity")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordConformity(int id, [FromBody] RecordConformityResource resource, CancellationToken cancellationToken)
    {
        if (!await evidenceAccess.ReceptionAsync(id, User, cancellationToken))
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var command = new RecordConformityCommand(id, resource.ComplianceNotes);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null)
                return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

            await physicalReceptionService.CloseConformantDispatchAsync(
                result,
                User.Identity?.Name ?? result.SiteResidentName,
                cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return ToErrorResult(ex);
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Report discrepancy".
    /// </summary>
    [HttpPost("{id:int}/discrepancy")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReportDiscrepancy(int id, [FromBody] ReportDiscrepancyResource resource, CancellationToken cancellationToken)
    {
        if (!await evidenceAccess.ReceptionAsync(id, User, cancellationToken))
            return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

        try
        {
            var command = new ReportDiscrepancyCommand(id, resource.Reason, resource.Description, resource.Severity);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null)
                return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return ToErrorResult(ex);
        }
    }

    /// <summary>
    /// Executes EventStorming Command: "Attach photographic evidence".
    /// </summary>
    [HttpPost("{id:int}/evidence")]
    [ProducesResponseType(typeof(ReceptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachEvidence(int id, [FromBody] AttachEvidenceResource resource, CancellationToken cancellationToken)
    {
        if(!await evidenceAccess.ReceptionAsync(id,User,cancellationToken))return NotFound();
        try
        {
            if (!Uri.TryCreate(resource.FileUrl,UriKind.Absolute,out var reference) || reference.Scheme is not ("https" or "http"))
                return BadRequest(new {code="EXTERNAL_REFERENCE_REQUIRED",message="Use multipart upload for stored photos; this endpoint accepts only external HTTP references."});
            var command = new AttachPhotographicEvidenceCommand(id, resource.EvidenceType, resource.FileUrl, resource.Description);
            var result = await receptionCommandService.Handle(command, cancellationToken);
            if (result is null)
                return NotFound(new ErrorResource(SiteReceptionErrorCodes.ReceptionNotFound, $"Reception {id} not found."));

            return Ok(ReceptionAssembler.ToResourceFromEntity(result));
        }
        catch (SiteReceptionException ex)
        {
            return ToErrorResult(ex);
        }
    }

    private IActionResult ToErrorResult(SiteReceptionException ex)
    {
        var body = new ErrorResource(ex.Code, ex.Message);
        return ex.Code switch
        {
            SiteReceptionErrorCodes.ReceptionNotFound or SiteReceptionErrorCodes.ItemNotFound => NotFound(body),
            SiteReceptionErrorCodes.ReceptionAlreadyClosed or SiteReceptionErrorCodes.DiscrepancyAlreadyReported => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
