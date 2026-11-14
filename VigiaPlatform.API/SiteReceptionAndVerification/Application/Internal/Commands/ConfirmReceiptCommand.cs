namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;

public record ConfirmReceiptCommand(
    int ReceptionId,
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
