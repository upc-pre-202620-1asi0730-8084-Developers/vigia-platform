namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.Rest.Resources;

public record DiscrepancyCaseResource(
    Guid Id,
    Guid DispatchId,
    Guid ReceptionId,
    string CaseNumber,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    string Status,
    string DeterminedLiability,
    string? ResolutionSummary
);