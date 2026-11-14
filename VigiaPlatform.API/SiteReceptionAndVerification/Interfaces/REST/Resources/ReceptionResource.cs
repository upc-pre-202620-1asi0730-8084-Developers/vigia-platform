namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Resources;

public record ScaleWeightResource(
    double GrossWeightKg,
    double TareWeightKg,
    double NetWeightKg,
    string WeighingSlipNumber,
    DateTimeOffset WeighedAt
);

public record ReceptionChecklistResource(
    bool IsSealIntact,
    bool IsDeliveryNotePhysicalOk,
    bool IsScaleCalibrated,
    bool IsFullyCompliant,
    string? Remarks
);

public record DigitalSignatureResource(
    string SignatureData,
    string SignedBy,
    DateTimeOffset SignedAt
);

public record DiscrepancyDetailsResource(
    string Reason,
    string Description,
    string Severity,
    DateTimeOffset ReportedAt
);

public record ReceptionItemResource(
    int Id,
    string MaterialId,
    string MaterialName,
    string Unit,
    double DispatchedQuantity,
    double ReceivedQuantity,
    double DiscrepancyQuantity,
    bool IsVerified,
    bool HasDiscrepancy,
    string? Comments
);

public record ReceptionEvidenceResource(
    int Id,
    string EvidenceType,
    string FileUrl,
    string Description,
    DateTimeOffset AttachedAt
);

public record ReceptionResource(
    int Id,
    string DispatchId,
    string CarrierPlate,
    string DriverName,
    int SiteId,
    string SiteName,
    string SiteResidentName,
    string Status,
    DateTimeOffset ArrivalRegisteredAt,
    string? DeliveryNoteNumber,
    string? ReceiverNotes,
    DateTimeOffset? ClosedAt,
    bool HasAnyDiscrepancy,
    ScaleWeightResource? ScaleWeight,
    ReceptionChecklistResource? Checklist,
    DigitalSignatureResource? DigitalSignature,
    DiscrepancyDetailsResource? Discrepancy,
    List<ReceptionItemResource> Items,
    List<ReceptionEvidenceResource> Evidences,
    string? VerifiedByUserId,
    bool IsPhysicalVerification,
    bool MaterialGoodCondition,
    bool QuantityVerified,
    bool DeliveryGuideReceived,
    bool ObservationsRecorded
);
