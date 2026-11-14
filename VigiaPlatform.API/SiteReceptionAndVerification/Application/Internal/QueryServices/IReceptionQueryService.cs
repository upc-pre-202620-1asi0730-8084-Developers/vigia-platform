namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.QueryServices;

using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Queries;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

public interface IReceptionQueryService
{
    Task<Reception?> Handle(GetReceptionByIdQuery query, CancellationToken cancellationToken = default);
    Task<Reception?> Handle(GetReceptionByDispatchIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reception>> Handle(GetAllReceptionsQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reception>> Handle(GetUpcomingArrivalsQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reception>> Handle(GetReceptionsBySiteIdQuery query, CancellationToken cancellationToken = default);
}
