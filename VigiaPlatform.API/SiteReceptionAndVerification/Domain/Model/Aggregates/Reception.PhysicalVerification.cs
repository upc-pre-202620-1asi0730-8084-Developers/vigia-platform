namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

public partial class Reception
{
    public string? VerifiedByUserId { get; private set; }
    public bool IsPhysicalVerification { get; private set; }
    public bool MaterialGoodCondition { get; private set; }
    public bool QuantityVerified { get; private set; }
    public bool DeliveryGuideReceived { get; private set; }
    public bool ObservationsRecorded { get; private set; }

    public void BeginPhysicalVerification(string guide, string actorId, string actorName)
    {
        EnsureNotClosed();
        if (Status != ReceptionStatus.UnitArrived) throw new InvalidOperationException("Arrival has already been confirmed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(guide);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorName);
        if (guide.Length > 50) throw new ArgumentException("Delivery guide must not exceed 50 characters.");
        DeliveryNoteNumber = guide.Trim();
        VerifiedByUserId = actorId;
        SiteResidentName = actorName;
        IsPhysicalVerification = true;
        Status = ReceptionStatus.ReceiptConfirmed;
    }

    public void CompletePhysicalVerification(bool goodCondition, bool quantityVerified, bool guideReceived,
        bool observationsRecorded, string? observations, bool reportDiscrepancy)
    {
        EnsureNotClosed();
        if (!IsPhysicalVerification || Status != ReceptionStatus.MaterialVerified || _items.Any(i => !i.IsVerified))
            throw new InvalidOperationException("Every material must be verified before completing physical reception.");
        if (!quantityVerified || !guideReceived) throw new ArgumentException("Quantity verification and the delivery guide are required.");
        if (observations?.Length > 500) throw new ArgumentException("Observations must not exceed 500 characters.");
        MaterialGoodCondition = goodCondition;
        QuantityVerified = quantityVerified;
        DeliveryGuideReceived = guideReceived;
        ObservationsRecorded = observationsRecorded;
        ReceiverNotes = observations;
        if (reportDiscrepancy || !goodCondition || _items.Any(i => i.HasDiscrepancy))
        {
            ReportDiscrepancy("PHYSICAL_VERIFICATION", string.IsNullOrWhiteSpace(observations) ? "Material quantity or condition differs from the dispatch guide." : observations);
            ClosedAt = DateTimeOffset.UtcNow;
        }
        else RecordConformity(observations);
    }

    public void SavePhysicalChecklist(bool goodCondition, bool quantityVerified, bool guideReceived, bool observationsRecorded, string? observations)
    {
        EnsureNotClosed();
        if (observations?.Length > 500) throw new ArgumentException("Observations must not exceed 500 characters.");
        MaterialGoodCondition = goodCondition;
        QuantityVerified = quantityVerified;
        DeliveryGuideReceived = guideReceived;
        ObservationsRecorded = observationsRecorded;
        ReceiverNotes = observations;
    }
}
