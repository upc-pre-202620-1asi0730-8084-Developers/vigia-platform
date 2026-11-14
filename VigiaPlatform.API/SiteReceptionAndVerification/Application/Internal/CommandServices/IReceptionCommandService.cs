namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.CommandServices;

using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

public interface IReceptionCommandService
{
    Task<Reception?> Handle(RecordUnitArrivalCommand command, CancellationToken cancellationToken = default);
    Task<Reception?> Handle(ConfirmReceiptCommand command, CancellationToken cancellationToken = default);
    Task<Reception?> Handle(VerifyReceivedMaterialCommand command, CancellationToken cancellationToken = default);
    Task<Reception?> Handle(RecordConformityCommand command, CancellationToken cancellationToken = default);
    Task<Reception?> Handle(ReportDiscrepancyCommand command, CancellationToken cancellationToken = default);
    Task<Reception?> Handle(AttachPhotographicEvidenceCommand command, CancellationToken cancellationToken = default);
}
