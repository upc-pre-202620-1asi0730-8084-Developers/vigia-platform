namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;

public record CreateDiscrepancyCaseCommand(
    Guid DispatchId,
    Guid ReceptionId,
    string CaseNumber
);