namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Value Object capturing the digital handwritten signature of the Site Resident upon delivery.
/// </summary>
public record DigitalSignature
{
    public string SignatureData { get; init; } = string.Empty;
    public string SignedBy { get; init; } = string.Empty;
    public DateTimeOffset SignedAt { get; init; }

    public DigitalSignature() { }

    public DigitalSignature(string signatureData, string signedBy, DateTimeOffset? signedAt = null)
    {
        if (string.IsNullOrWhiteSpace(signatureData))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidSignature, "Signature data is required.");
        if (string.IsNullOrWhiteSpace(signedBy))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidSignature, "Signer identity is required.");

        SignatureData = signatureData.Trim();
        SignedBy = signedBy.Trim();
        SignedAt = signedAt ?? DateTimeOffset.UtcNow;
    }
}
