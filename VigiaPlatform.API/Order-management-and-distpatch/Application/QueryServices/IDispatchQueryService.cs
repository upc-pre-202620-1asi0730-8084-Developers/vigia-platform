using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;

public interface IDispatchQueryService
{
    Task<IEnumerable<Dispatch>> Handle(GetAllDispatchesQuery query, CancellationToken cancellationToken = default);
    Task<Dispatch?> Handle(GetDispatchByIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Dispatch>> Handle(GetDispatchesByStatusQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Dispatch>> Handle(GetDispatchesByDepartureDateQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Dispatch>> Handle(GetDispatchesByRelatedOrderQuery query, CancellationToken cancellationToken = default);
}
