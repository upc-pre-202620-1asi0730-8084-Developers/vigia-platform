using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.CommandServices;

public interface IDiscrepancyCaseCommandService
{
    Task<DiscrepancyCase> Handle(
        CreateDiscrepancyCaseCommand command,
        CancellationToken cancellationToken = default);

    Task<DiscrepancyCase?> Handle(
        DetermineDiscrepancyResponsibilityCommand command,
        CancellationToken cancellationToken = default);

    Task<DiscrepancyCase?> Handle(
        CloseDiscrepancyCaseCommand command,
        CancellationToken cancellationToken = default);
}