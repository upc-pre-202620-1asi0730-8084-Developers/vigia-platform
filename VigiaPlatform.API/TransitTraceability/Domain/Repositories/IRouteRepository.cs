using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Repositories;

public interface IRouteRepository : IBaseRepository<TransitRoute>
{
    /// <summary>Finds a route with its waypoints, positions and alerts loaded.</summary>
    Task<TransitRoute?> FindByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists routes with their waypoints, positions and alerts loaded, optionally filtered by status.</summary>
    Task<IEnumerable<TransitRoute>> ListWithDetailsAsync(RouteStatus? status = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsByDispatchIdAsync(string dispatchId, CancellationToken cancellationToken = default);
}
