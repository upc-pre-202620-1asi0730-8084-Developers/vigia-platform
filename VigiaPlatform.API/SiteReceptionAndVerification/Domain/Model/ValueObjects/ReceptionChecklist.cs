namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

/// <summary>
/// Value Object capturing physical custody verification items at arrival.
/// </summary>
public record ReceptionChecklist
{
    public bool IsSealIntact { get; init; }
    public bool IsDeliveryNotePhysicalOk { get; init; }
    public bool IsScaleCalibrated { get; init; }
    public string? Remarks { get; init; }

    public bool IsFullyCompliant => IsSealIntact && IsDeliveryNotePhysicalOk && IsScaleCalibrated;

    public ReceptionChecklist() { }

    public ReceptionChecklist(bool isSealIntact, bool isDeliveryNotePhysicalOk, bool isScaleCalibrated, string? remarks = null)
    {
        IsSealIntact = isSealIntact;
        IsDeliveryNotePhysicalOk = isDeliveryNotePhysicalOk;
        IsScaleCalibrated = isScaleCalibrated;
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }
}
