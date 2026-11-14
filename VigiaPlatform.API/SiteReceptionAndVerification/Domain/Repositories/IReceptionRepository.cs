namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Repositories;

using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

/// <summary>
/// Domain repository interface for the Reception Aggregate Root.
/// </summary>
public interface IReceptionRepository : IBaseRepository<Reception>
{
    Task<Reception?> FindByDispatchIdAsync(string dispatchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reception>> ListBySiteIdAsync(int siteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reception>> ListUpcomingArrivalsAsync(CancellationToken cancellationToken = default);
}
