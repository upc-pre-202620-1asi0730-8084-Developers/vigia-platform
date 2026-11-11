namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;

public record CreateDiscrepancyCaseCommand(
    string DispatchId,
    string ReceptionId,
    string CaseNumber
);