using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Queries;

namespace VigiaPlatform.API.TransitTraceability.Domain.Services;

public interface IRouteQueryService
{
    Task<IEnumerable<TransitRoute>> Handle(GetAllRoutesQuery query, CancellationToken cancellationToken = default);

    Task<TransitRoute?> Handle(GetRouteByIdQuery query, CancellationToken cancellationToken = default);
}
