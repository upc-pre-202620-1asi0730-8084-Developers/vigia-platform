using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.CommandServices;

public class DiscrepancyCaseCommandService(
    IDiscrepancyCaseRepository repository,
    IUnitOfWork unitOfWork)
    : IDiscrepancyCaseCommandService
{
    public async Task<DiscrepancyCase> Handle(
        CreateDiscrepancyCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var discrepancyCase = new DiscrepancyCase(
            command.DispatchId,
            command.ReceptionId,
            command.CaseNumber
        );

        await repository.AddAsync(
            discrepancyCase,
            cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);

        return discrepancyCase;
    }

    public async Task<DiscrepancyCase?> Handle(
        DetermineDiscrepancyResponsibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var discrepancyCase = await repository.FindByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (discrepancyCase is null)
        {
            return null;
        }

        discrepancyCase.DetermineResponsibility(
            command.Responsibility);

        await unitOfWork.CompleteAsync(cancellationToken);

        return discrepancyCase;
    }

    public async Task<DiscrepancyCase?> Handle(
        CloseDiscrepancyCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var discrepancyCase = await repository.FindByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (discrepancyCase is null)
        {
            return null;
        }

        discrepancyCase.Close(command.ResolutionSummary);

        await unitOfWork.CompleteAsync(cancellationToken);

        return discrepancyCase;
    }
}
