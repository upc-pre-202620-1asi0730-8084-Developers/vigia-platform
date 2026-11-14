namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

public record DetermineDiscrepancyResponsibilityResource(
    string? Responsibility,
    string? Status,
    string? ResolutionSummary
);