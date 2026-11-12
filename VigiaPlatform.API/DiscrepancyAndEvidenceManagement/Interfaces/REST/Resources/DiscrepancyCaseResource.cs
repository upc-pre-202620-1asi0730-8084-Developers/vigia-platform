namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

public record DiscrepancyCaseResource(
    int Id,
    string DispatchId,
    string ReceptionId,
    string CaseNumber,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    string Status,
    string DeterminedLiability,
    string? ResolutionSummary
)
{
    public string? Responsibility =>
        DeterminedLiability == "Pending"
            ? null
            : DeterminedLiability;
}