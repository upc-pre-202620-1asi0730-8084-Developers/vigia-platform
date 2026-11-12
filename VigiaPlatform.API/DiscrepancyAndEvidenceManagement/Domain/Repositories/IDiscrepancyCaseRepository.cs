using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;

public interface IDiscrepancyCaseRepository
{
    Task<IEnumerable<DiscrepancyCase>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<DiscrepancyCase?> FindByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DiscrepancyCase discrepancyCase,
        CancellationToken cancellationToken = default);
}