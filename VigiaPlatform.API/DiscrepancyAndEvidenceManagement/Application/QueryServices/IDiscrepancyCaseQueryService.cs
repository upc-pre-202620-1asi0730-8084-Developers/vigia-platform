using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Queries;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;

public interface IDiscrepancyCaseQueryService
{
    Task<IEnumerable<DiscrepancyCase>> Handle(
        GetAllDiscrepancyCasesQuery query,
        CancellationToken cancellationToken = default);
}