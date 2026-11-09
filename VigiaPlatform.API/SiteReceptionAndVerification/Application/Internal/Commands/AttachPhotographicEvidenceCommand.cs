namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;

public record AttachPhotographicEvidenceCommand(
    int ReceptionId,
    string EvidenceType,
    string FileUrl,
    string Description
);
