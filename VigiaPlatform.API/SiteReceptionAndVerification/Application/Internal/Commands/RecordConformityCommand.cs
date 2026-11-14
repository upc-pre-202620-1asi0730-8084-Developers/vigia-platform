namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;

public record RecordConformityCommand(
    int ReceptionId,
    string? ComplianceNotes = null
);
