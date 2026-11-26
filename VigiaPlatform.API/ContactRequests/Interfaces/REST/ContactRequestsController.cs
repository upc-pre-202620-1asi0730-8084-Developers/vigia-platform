namespace VigiaPlatform.API.ContactRequests.Interfaces.REST;

using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VigiaPlatform.API.ContactRequests.Application;
using VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates;

public sealed record CreateContactRequestResource(
    string? RequestId,
    string? FullName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Segment,
    string? Plan,
    string? Message,
    string? Locale);

public sealed record ContactRequestResource(
    long Id,
    string? RequestId,
    string FullName,
    string CompanyName,
    string Email,
    string? Phone,
    string Segment,
    string? Plan,
    string Message,
    string Locale,
    string Status,
    DateTime CreatedAtUtc);

[ApiController]
[Route("api/v1/contact-requests")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Contact Requests")]
public sealed class ContactRequestsController(ContactRequestService service) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("contact-requests")]
    [ProducesResponseType(typeof(ContactRequestResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateContactRequestResource? resource, CancellationToken ct)
    {
        if (resource is null)
        {
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                message = "Contact request payload is required.",
                errors = new Dictionary<string, string[]>
                {
                    ["body"] = ["Request body cannot be empty."]
                }
            });
        }

        var result = await service.CreateAsync(
            new CreateContactRequestCommand(
                resource.RequestId,
                resource.FullName,
                resource.CompanyName,
                resource.Email,
                resource.Phone,
                resource.Segment,
                resource.Plan,
                resource.Message,
                resource.Locale),
            ct);

        if (result.ValidationErrors is not null)
        {
            var missingFields = string.Join(", ", result.ValidationErrors.Keys);
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                message = $"Invalid or missing required contact fields: {missingFields}.",
                errors = result.ValidationErrors
            });
        }

        if (result.ConflictCode is not null)
        {
            return Conflict(new
            {
                code = result.ConflictCode,
                message = result.ConflictMessage
            });
        }

        var dto = ToResource(result.Request!);
        return Created($"/api/v1/contact-requests/{dto.Id}", dto);
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<ContactRequestResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await service.ListAsync(ct);
        return Ok(items.Select(ToResource).ToList());
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ContactRequestResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var item = await service.FindByIdAsync(id, ct);
        return item is null
            ? NotFound(new { code = "NOT_FOUND", message = $"Contact request {id} was not found." })
            : Ok(ToResource(item));
    }

    private static ContactRequestResource ToResource(ContactRequest x) => new(
        x.Id,
        x.RequestId,
        x.FullName,
        x.CompanyName,
        x.Email,
        x.Phone,
        x.Segment,
        x.Plan,
        x.Message,
        x.Locale,
        x.Status,
        x.CreatedAtUtc);
}
