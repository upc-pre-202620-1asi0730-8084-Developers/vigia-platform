namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;

public record DetermineDiscrepancyResponsibilityCommand(
    int Id,
    string Responsibility
);