namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

using VigiaPlatform.API.Shared.Domain.Model;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

/// <summary>
/// Reception Aggregate Root for the Site Reception and Verification Bounded Context.
/// Orchestrates the entire on-site verification flow modeled in EventStorming:
/// 1. Record unit arrival
/// 2. Confirm receipt (cross-check against delivery note and scale weight)
/// 3. Verify received material (comparison of dispatched vs received quantities)
/// 4. Conformity assessment: Record compliance (office closed) OR Report discrepancy & Attach evidence.
/// </summary>
public class Reception : IAuditableEntity
{
    private readonly List<ReceptionItem> _items = [];
    private readonly List<ReceptionEvidence> _evidences = [];

    public int Id { get; private set; }

    /// <summary>Reference to Order Management and Dispatch BC.</summary>
    public string DispatchId { get; private set; } = string.Empty;

    public string CarrierPlate { get; private set; } = string.Empty;
    public string DriverName { get; private set; } = string.Empty;
    public int SiteId { get; private set; }
    public string SiteName { get; private set; } = string.Empty;
    public string SiteResidentName { get; private set; } = string.Empty;

    public ReceptionStatus Status { get; private set; }
    public DateTimeOffset ArrivalRegisteredAt { get; private set; }

    public ScaleWeight? ScaleWeight { get; private set; }
    public ReceptionChecklist? Checklist { get; private set; }
    public DigitalSignature? DigitalSignature { get; private set; }
    public DiscrepancyDetails? Discrepancy { get; private set; }

    public string? DeliveryNoteNumber { get; private set; }
    public string? ReceiverNotes { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public IReadOnlyList<ReceptionItem> Items => _items.AsReadOnly();
    public IReadOnlyList<ReceptionEvidence> Evidences => _evidences.AsReadOnly();

    public bool HasAnyDiscrepancy => _items.Any(i => i.HasDiscrepancy) || Discrepancy is not null;

    private Reception() { }

    /// <summary>
    /// Executes the "Record unit arrival" command.
    /// Creates a new reception record when the transport unit enters the site geofence / gate.
    /// </summary>
    public Reception(string dispatchId, string carrierPlate, string driverName, int siteId, string siteName, string siteResidentName, DateTimeOffset? arrivalTime = null)
    {
        if (string.IsNullOrWhiteSpace(dispatchId))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Dispatch ID is required.");
        if (string.IsNullOrWhiteSpace(carrierPlate))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Carrier vehicle plate is required.");
        if (string.IsNullOrWhiteSpace(driverName))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Driver name is required.");
        if (siteId <= 0)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Valid Site ID is required.");
        if (string.IsNullOrWhiteSpace(siteName))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Site name is required.");
        if (string.IsNullOrWhiteSpace(siteResidentName))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Site resident name is required.");

        DispatchId = dispatchId.Trim();
        CarrierPlate = carrierPlate.Trim();
        DriverName = driverName.Trim();
        SiteId = siteId;
        SiteName = siteName.Trim();
        SiteResidentName = siteResidentName.Trim();

        ArrivalRegisteredAt = arrivalTime ?? DateTimeOffset.UtcNow;
        Status = ReceptionStatus.UnitArrived;
    }

    /// <summary>
    /// Adds expected material line items from the dispatch guide prior to or upon arrival.
    /// </summary>
    public ReceptionItem AddItem(string materialId, string materialName, string unit, double dispatchedQuantity)
    {
        EnsureNotClosed();
        var item = new ReceptionItem(materialId, materialName, unit, dispatchedQuantity);
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Executes the "Confirm Receipt" command.
    /// Records scale weights, physical custody checklist and resident digital signature.
    /// </summary>
    public void ConfirmReceipt(string deliveryNoteNumber, ScaleWeight scaleWeight, ReceptionChecklist checklist, DigitalSignature digitalSignature, string? receiverNotes = null)
    {
        EnsureNotClosed();

        if (Status != ReceptionStatus.UnitArrived)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidStateTransition,
                $"Cannot confirm receipt when in status '{Status}'. Unit arrival must be registered first.");

        if (string.IsNullOrWhiteSpace(deliveryNoteNumber))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Delivery note number is required.");

        DeliveryNoteNumber = deliveryNoteNumber.Trim();
        ScaleWeight = scaleWeight ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidWeight, "Scale weight details are required.");
        Checklist = checklist ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Checklist details are required.");
        DigitalSignature = digitalSignature ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidSignature, "Digital signature is required.");
        ReceiverNotes = string.IsNullOrWhiteSpace(receiverNotes) ? null : receiverNotes.Trim();

        Status = ReceptionStatus.ReceiptConfirmed;
    }

    /// <summary>
    /// Executes the "Verify received material" command for a specific material line item.
    /// </summary>
    public void VerifyMaterial(string materialId, double receivedQuantity, string? comments = null)
    {
        EnsureNotClosed();

        if (Status != ReceptionStatus.ReceiptConfirmed && Status != ReceptionStatus.MaterialVerified)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidStateTransition,
                $"Cannot verify materials when reception is in status '{Status}'. Receipt must be confirmed first.");

        var item = _items.FirstOrDefault(i => i.MaterialId.Equals(materialId, StringComparison.OrdinalIgnoreCase))
                   ?? throw new SiteReceptionException(SiteReceptionErrorCodes.ItemNotFound,
                       $"Material item '{materialId}' not found in reception {Id}.");

        item.Verify(receivedQuantity, comments);

        if (_items.All(i => i.IsVerified))
        {
            Status = ReceptionStatus.MaterialVerified;
        }
    }

    /// <summary>
    /// Executes the "Registrar conformidad" command.
    /// Finalizes reception when quantities and condition are conformant ("Office closed").
    /// </summary>
    public void RecordConformity(string? complianceNotes = null)
    {
        EnsureNotClosed();

        if (Status != ReceptionStatus.MaterialVerified && Status != ReceptionStatus.ReceiptConfirmed)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidStateTransition,
                $"Cannot record conformity from status '{Status}'.");

        if (Checklist is not null && !Checklist.IsSealIntact)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidStateTransition,
                "Cannot register full compliance if the security seal was flagged as broken or tampered. A discrepancy must be reported.");

        ReceiverNotes = string.IsNullOrWhiteSpace(complianceNotes) ? ReceiverNotes : complianceNotes.Trim();
        Status = ReceptionStatus.ConformantClosed;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Executes the "Report discrepancy" command when physical differences, mermas or broken seals are detected.
    /// </summary>
    public void ReportDiscrepancy(string reason, string description, string severity = "Major")
    {
        EnsureNotClosed();

        if (Status != ReceptionStatus.ReceiptConfirmed && Status != ReceptionStatus.MaterialVerified)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidStateTransition,
                $"Cannot report a discrepancy from status '{Status}'.");

        if (Discrepancy is not null)
            throw new SiteReceptionException(SiteReceptionErrorCodes.DiscrepancyAlreadyReported,
                $"Discrepancy has already been reported for reception {Id}.");

        Discrepancy = new DiscrepancyDetails(reason, description, severity);
        Status = ReceptionStatus.DiscrepancyReported;
    }

    /// <summary>
    /// Executes the "Attach photographic evidence" command.
    /// </summary>
    public ReceptionEvidence AttachEvidence(string evidenceType, string fileUrl, string description)
    {
        EnsureNotClosed();

        var evidence = new ReceptionEvidence(evidenceType, fileUrl, description);
        _evidences.Add(evidence);
        return evidence;
    }

    /// <summary>
    /// Rejects the reception outright at the gate due to severe invalidity.
    /// </summary>
    public void Reject(string reason)
    {
        EnsureNotClosed();
        Discrepancy = new DiscrepancyDetails("REJECTION", reason, "Critical");
        Status = ReceptionStatus.Rejected;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureNotClosed()
    {
        if (Status == ReceptionStatus.ConformantClosed)
            throw new SiteReceptionException(SiteReceptionErrorCodes.ReceptionAlreadyClosed,
                $"Reception {Id} is already closed conformant and cannot be modified.");
    }
}
