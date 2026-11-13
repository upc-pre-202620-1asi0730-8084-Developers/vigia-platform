namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Transform;

using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Resources;

public static class ReceptionAssembler
{
    public static RecordUnitArrivalCommand ToCommandFromResource(RecordUnitArrivalResource resource)
    {
        var items = resource.Items?.Select(i => new InitialItemRecord(i.MaterialId, i.MaterialName, i.Unit, i.DispatchedQuantity)).ToList();
        return new RecordUnitArrivalCommand(
            resource.DispatchId,
            resource.CarrierPlate,
            resource.DriverName,
            resource.SiteId,
            resource.SiteName,
            resource.SiteResidentName,
            items
        );
    }

    public static ConfirmReceiptCommand ToCommandFromResource(int receptionId, ConfirmReceiptResource resource)
    {
        return new ConfirmReceiptCommand(
            receptionId,
            resource.DeliveryNoteNumber,
            resource.GrossWeightKg,
            resource.TareWeightKg,
            resource.WeighingSlipNumber,
            resource.IsSealIntact,
            resource.IsDeliveryNotePhysicalOk,
            resource.IsScaleCalibrated,
            resource.ChecklistRemarks,
            resource.SignatureData,
            resource.SignedBy,
            resource.ReceiverNotes
        );
    }

    public static ReceptionResource ToResourceFromEntity(Reception entity)
    {
        return new ReceptionResource(
            entity.Id,
            entity.DispatchId,
            entity.CarrierPlate,
            entity.DriverName,
            entity.SiteId,
            entity.SiteName,
            entity.SiteResidentName,
            entity.Status.ToString(),
            entity.ArrivalRegisteredAt,
            entity.DeliveryNoteNumber,
            entity.ReceiverNotes,
            entity.ClosedAt,
            entity.HasAnyDiscrepancy,
            entity.ScaleWeight is null ? null : new ScaleWeightResource(
                entity.ScaleWeight.GrossWeightKg,
                entity.ScaleWeight.TareWeightKg,
                entity.ScaleWeight.NetWeightKg,
                entity.ScaleWeight.WeighingSlipNumber,
                entity.ScaleWeight.WeighedAt
            ),
            entity.Checklist is null ? null : new ReceptionChecklistResource(
                entity.Checklist.IsSealIntact,
                entity.Checklist.IsDeliveryNotePhysicalOk,
                entity.Checklist.IsScaleCalibrated,
                entity.Checklist.IsFullyCompliant,
                entity.Checklist.Remarks
            ),
            entity.DigitalSignature is null ? null : new DigitalSignatureResource(
                entity.DigitalSignature.SignatureData,
                entity.DigitalSignature.SignedBy,
                entity.DigitalSignature.SignedAt
            ),
            entity.Discrepancy is null ? null : new DiscrepancyDetailsResource(
                entity.Discrepancy.Reason,
                entity.Discrepancy.Description,
                entity.Discrepancy.Severity,
                entity.Discrepancy.ReportedAt
            ),
            entity.Items.Select(ToItemResource).ToList(),
            entity.Evidences.Select(ToEvidenceResource).ToList()
        );
    }

    public static ReceptionItemResource ToItemResource(ReceptionItem item)
    {
        return new ReceptionItemResource(
            item.Id,
            item.MaterialId,
            item.MaterialName,
            item.Unit,
            item.DispatchedQuantity,
            item.ReceivedQuantity,
            item.DiscrepancyQuantity,
            item.IsVerified,
            item.HasDiscrepancy,
            item.Comments
        );
    }

    public static ReceptionEvidenceResource ToEvidenceResource(ReceptionEvidence evidence)
    {
        return new ReceptionEvidenceResource(
            evidence.Id,
            evidence.EvidenceType,
            evidence.FileUrl,
            evidence.Description,
            evidence.AttachedAt
        );
    }
}
