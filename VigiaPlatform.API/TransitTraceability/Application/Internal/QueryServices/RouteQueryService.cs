using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Queries;
using VigiaPlatform.API.TransitTraceability.Domain.Repositories;
using VigiaPlatform.API.TransitTraceability.Domain.Services;

namespace VigiaPlatform.API.TransitTraceability.Application.Internal.QueryServices;

public class RouteQueryService(IRouteRepository routeRepository) : IRouteQueryService
{
    public async Task<IEnumerable<TransitRoute>> Handle(GetAllRoutesQuery query, CancellationToken cancellationToken = default)
    {
        return await routeRepository.ListWithDetailsAsync(query.Status, cancellationToken);
    }

    public async Task<TransitRoute?> Handle(GetRouteByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await routeRepository.FindByIdWithDetailsAsync(query.RouteId, cancellationToken);
    }
}
