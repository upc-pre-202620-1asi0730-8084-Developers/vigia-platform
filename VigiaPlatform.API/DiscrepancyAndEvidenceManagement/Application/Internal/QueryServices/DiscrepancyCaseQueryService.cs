using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Queries;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.QueryServices;

public class DiscrepancyCaseQueryService(
    IDiscrepancyCaseRepository discrepancyCaseRepository)
    : IDiscrepancyCaseQueryService
{
    public async Task<IEnumerable<DiscrepancyCase>> Handle(
        GetAllDiscrepancyCasesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await discrepancyCaseRepository.ListAsync(
            cancellationToken);
    }

    public async Task<DiscrepancyCase?> Handle(
        GetDiscrepancyCaseByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await discrepancyCaseRepository.FindByIdAsync(
            query.Id,
            cancellationToken);
    }

    public async Task<DiscrepancyCase?> Handle(
        GetDiscrepancyCaseTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        return await discrepancyCaseRepository.FindByIdWithEventsAsync(
            query.Id,
            cancellationToken);
    }
}