namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.CommandServices;

using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Repositories;

public class ReceptionCommandService(IReceptionRepository receptionRepository, IUnitOfWork unitOfWork) : IReceptionCommandService
{
    public async Task<Reception?> Handle(RecordUnitArrivalCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await receptionRepository.FindByDispatchIdAsync(command.DispatchId, cancellationToken);
        if (existing is not null)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival,
                $"A reception for dispatch '{command.DispatchId}' is already registered.");

        var reception = new Reception(
            command.DispatchId,
            command.CarrierPlate,
            command.DriverName,
            command.SiteId,
            command.SiteName,
            command.SiteResidentName
        );

        if (command.Items is not null)
        {
            foreach (var item in command.Items)
            {
                reception.AddItem(item.MaterialId, item.MaterialName, item.Unit, item.DispatchedQuantity);
            }
        }

        await receptionRepository.AddAsync(reception, cancellationToken);
        await unitOfWork.CompleteAsync();
        return reception;
    }

    public async Task<Reception?> Handle(ConfirmReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var reception = await receptionRepository.FindByIdAsync(command.ReceptionId, cancellationToken)
            ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, $"Reception {command.ReceptionId} not found.");

        var scaleWeight = new ScaleWeight(
            command.GrossWeightKg,
            command.TareWeightKg,
            command.WeighingSlipNumber
        );

        var checklist = new ReceptionChecklist(
            command.IsSealIntact,
            command.IsDeliveryNotePhysicalOk,
            command.IsScaleCalibrated,
            command.ChecklistRemarks
        );

        var signature = new DigitalSignature(
            command.SignatureData,
            command.SignedBy
        );

        reception.ConfirmReceipt(command.DeliveryNoteNumber, scaleWeight, checklist, signature, command.ReceiverNotes);

        receptionRepository.Update(reception);
        await unitOfWork.CompleteAsync();
        return reception;
    }

    public async Task<Reception?> Handle(VerifyReceivedMaterialCommand command, CancellationToken cancellationToken = default)
    {
        var reception = await receptionRepository.FindByIdAsync(command.ReceptionId, cancellationToken)
            ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, $"Reception {command.ReceptionId} not found.");

        reception.VerifyMaterial(command.MaterialId, command.ReceivedQuantity, command.Comments);

        receptionRepository.Update(reception);
        await unitOfWork.CompleteAsync();
        return reception;
    }

    public async Task<Reception?> Handle(RecordConformityCommand command, CancellationToken cancellationToken = default)
    {
        var reception = await receptionRepository.FindByIdAsync(command.ReceptionId, cancellationToken)
            ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, $"Reception {command.ReceptionId} not found.");

        reception.RecordConformity(command.ComplianceNotes);

        receptionRepository.Update(reception);
        await unitOfWork.CompleteAsync();
        return reception;
    }

    public async Task<Reception?> Handle(ReportDiscrepancyCommand command, CancellationToken cancellationToken = default)
    {
        var reception = await receptionRepository.FindByIdAsync(command.ReceptionId, cancellationToken)
            ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, $"Reception {command.ReceptionId} not found.");

        reception.ReportDiscrepancy(command.Reason, command.Description, command.Severity);

        receptionRepository.Update(reception);
        await unitOfWork.CompleteAsync();
        return reception;
    }

    public async Task<Reception?> Handle(AttachPhotographicEvidenceCommand command, CancellationToken cancellationToken = default)
    {
        var reception = await receptionRepository.FindByIdAsync(command.ReceptionId, cancellationToken)
            ?? throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, $"Reception {command.ReceptionId} not found.");

        reception.AttachEvidence(command.EvidenceType, command.FileUrl, command.Description);

        receptionRepository.Update(reception);
        await unitOfWork.CompleteAsync();
        return reception;
    }
}
