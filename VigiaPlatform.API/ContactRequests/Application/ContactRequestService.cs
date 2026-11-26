namespace VigiaPlatform.API.ContactRequests.Application;

using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates;
using VigiaPlatform.API.ContactRequests.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

public sealed record CreateContactRequestCommand(
    string? RequestId,
    string? FullName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Segment,
    string? Plan,
    string? Message,
    string? Locale);

public sealed record ContactRequestResult(
    ContactRequest? Request,
    IReadOnlyDictionary<string, string[]>? ValidationErrors,
    string? ConflictCode,
    string? ConflictMessage);

public sealed class ContactRequestService(
    IContactRequestRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<ContactRequestResult> CreateAsync(CreateContactRequestCommand command, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void AddError(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                list = [];
                errors[field] = list;
            }
            list.Add(message);
        }

        var fullName = (command.FullName ?? string.Empty).Trim();
        var companyName = (command.CompanyName ?? string.Empty).Trim();
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim();
        var segment = (command.Segment ?? string.Empty).Trim().ToUpperInvariant();
        var plan = string.IsNullOrWhiteSpace(command.Plan) ? null : command.Plan.Trim().ToUpperInvariant();
        var message = (command.Message ?? string.Empty).Trim();
        var locale = NormalizeLocale(command.Locale);
        var requestId = string.IsNullOrWhiteSpace(command.RequestId) ? null : command.RequestId.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            AddError("fullName", "Full name is required.");
        else if (fullName.Length is < 2 or > 120)
            AddError("fullName", "Full name must be between 2 and 120 characters.");

        if (string.IsNullOrWhiteSpace(companyName))
            AddError("companyName", "Company name is required.");
        else if (companyName.Length is < 2 or > 160)
            AddError("companyName", "Company name must be between 2 and 160 characters.");

        if (string.IsNullOrWhiteSpace(email))
            AddError("email", "Work email is required.");
        else if (email.Length > 180 || !IsValidEmail(email))
            AddError("email", "Work email must be a valid email address.");

        if (phone is not null && phone.Length > 40)
            AddError("phone", "Phone number cannot exceed 40 characters.");

        if (string.IsNullOrWhiteSpace(segment))
            AddError("segment", "Business segment is required.");
        else if (!ContactRequest.AllowedSegments.Contains(segment))
            AddError("segment", "Business segment must be CONSTRUCTION, LOGISTICS, or OTHER.");

        if (plan is not null && !ContactRequest.AllowedPlans.Contains(plan))
            AddError("plan", "Plan must be STARTER, PRO, ENTERPRISE, or CUSTOM.");

        if (string.IsNullOrWhiteSpace(message))
            AddError("message", "Message is required.");
        else if (message.Length is < 10 or > 1500)
            AddError("message", "Message must be between 10 and 1500 characters.");

        if (requestId is not null && (!Guid.TryParse(requestId, out var parsed) || parsed == Guid.Empty))
            AddError("requestId", "RequestId must be a valid non-empty UUID.");
        else if (requestId is not null)
            requestId = Guid.Parse(requestId).ToString("D");

        if (errors.Count > 0)
        {
            return new ContactRequestResult(
                null,
                errors.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.Ordinal),
                null,
                null);
        }

        if (requestId is not null)
        {
            var existing = await repository.FindByRequestIdAsync(requestId, ct);
            if (existing is not null)
            {
                return existing.MatchesPayload(fullName, companyName, email, phone, segment, plan, message)
                    ? new ContactRequestResult(existing, null, null, null)
                    : new ContactRequestResult(null, null, "REQUEST_ID_CONFLICT",
                        "This requestId was already used with a different contact request payload.");
            }
        }

        var entity = new ContactRequest(
            requestId,
            fullName,
            companyName,
            email,
            phone,
            segment,
            plan,
            message,
            locale,
            DateTime.UtcNow);

        await repository.AddAsync(entity, ct);

        try
        {
            await unitOfWork.CompleteAsync();
            return new ContactRequestResult(entity, null, null, null);
        }
        catch (DbUpdateException ex) when (requestId is not null && ex.InnerException is MySqlException { Number: 1062 })
        {
            var existing = await repository.FindByRequestIdAsync(requestId, ct);
            if (existing is not null && existing.MatchesPayload(fullName, companyName, email, phone, segment, plan, message))
                return new ContactRequestResult(existing, null, null, null);

            return new ContactRequestResult(null, null, "REQUEST_ID_CONFLICT",
                "This requestId was already used with a different contact request payload.");
        }
    }

    public Task<IReadOnlyList<ContactRequest>> ListAsync(CancellationToken ct = default) =>
        repository.ListAsync(ct);

    public Task<ContactRequest?> FindByIdAsync(long id, CancellationToken ct = default) =>
        repository.FindByIdAsync(id, ct);

    private static bool IsValidEmail(string value)
    {
        if (!value.Contains('@') || value.EndsWith('.') || value.StartsWith('.'))
            return false;
        try
        {
            var addr = new MailAddress(value);
            var host = addr.Host;
            return string.Equals(addr.Address, value, StringComparison.OrdinalIgnoreCase)
                && host.Contains('.')
                && !host.StartsWith('.')
                && !host.EndsWith('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeLocale(string? value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        return raw.StartsWith("en", StringComparison.Ordinal) ? "en" : "es";
    }
}
