namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;

public record CloseDiscrepancyCaseCommand(
    int Id,
    string? ResolutionSummary
);