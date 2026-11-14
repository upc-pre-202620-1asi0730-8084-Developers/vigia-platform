namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Value Object summarizing a reported material or logistical discrepancy.
/// </summary>
public record DiscrepancyDetails
{
    public string Reason { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = "Major";
    public DateTimeOffset ReportedAt { get; init; }

    public DiscrepancyDetails() { }

    public DiscrepancyDetails(string reason, string description, string severity = "Major", DateTimeOffset? reportedAt = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidDiscrepancy, "Discrepancy reason is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidDiscrepancy, "Discrepancy description is required.");

        Reason = reason.Trim();
        Description = description.Trim();
        Severity = string.IsNullOrWhiteSpace(severity) ? "Major" : severity.Trim();
        ReportedAt = reportedAt ?? DateTimeOffset.UtcNow;
    }
}
