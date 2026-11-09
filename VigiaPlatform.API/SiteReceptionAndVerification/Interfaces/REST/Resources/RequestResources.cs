namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Resources;

public record InitialItemResource(
    string MaterialId,
    string MaterialName,
    string Unit,
    double DispatchedQuantity
);

public record RecordUnitArrivalResource(
    string DispatchId,
    string CarrierPlate,
    string DriverName,
    int SiteId,
    string SiteName,
    string SiteResidentName,
    List<InitialItemResource>? Items = null
);

public record ConfirmReceiptResource(
    string DeliveryNoteNumber,
    double GrossWeightKg,
    double TareWeightKg,
    string WeighingSlipNumber,
    bool IsSealIntact,
    bool IsDeliveryNotePhysicalOk,
    bool IsScaleCalibrated,
    string? ChecklistRemarks,
    string SignatureData,
    string SignedBy,
    string? ReceiverNotes = null
);

public record VerifyMaterialResource(
    string MaterialId,
    double ReceivedQuantity,
    string? Comments = null
);

public record RecordConformityResource(
    string? ComplianceNotes = null
);

public record ReportDiscrepancyResource(
    string Reason,
    string Description,
    string Severity = "Major"
);

public record AttachEvidenceResource(
    string EvidenceType,
    string FileUrl,
    string Description
);
