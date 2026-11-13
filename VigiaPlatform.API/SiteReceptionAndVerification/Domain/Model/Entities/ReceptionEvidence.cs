namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Entity representing photographic or technical evidence attached to a reception or discrepancy.
/// </summary>
public class ReceptionEvidence
{
    public int Id { get; private set; }
    public int ReceptionId { get; private set; }
    public string EvidenceType { get; private set; } = string.Empty;
    public string FileUrl { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset AttachedAt { get; private set; }

    private ReceptionEvidence() { }

    public ReceptionEvidence(string evidenceType, string fileUrl, string description, DateTimeOffset? attachedAt = null)
    {
        if (string.IsNullOrWhiteSpace(evidenceType))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidEvidence, "Evidence type is required.");
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidEvidence, "File URL or path is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidEvidence, "Evidence description is required.");

        EvidenceType = evidenceType.Trim().ToUpperInvariant();
        FileUrl = fileUrl.Trim();
        Description = description.Trim();
        AttachedAt = attachedAt ?? DateTimeOffset.UtcNow;
    }
}
