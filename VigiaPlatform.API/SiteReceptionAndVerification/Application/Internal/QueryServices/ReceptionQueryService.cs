namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.QueryServices;

using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Queries;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Repositories;

public class ReceptionQueryService(IReceptionRepository receptionRepository) : IReceptionQueryService
{
    public async Task<Reception?> Handle(GetReceptionByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await receptionRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<Reception?> Handle(GetReceptionByDispatchIdQuery query, CancellationToken cancellationToken = default)
    {
        return await receptionRepository.FindByDispatchIdAsync(query.DispatchId, cancellationToken);
    }

    public async Task<IEnumerable<Reception>> Handle(GetAllReceptionsQuery query, CancellationToken cancellationToken = default)
    {
        return await receptionRepository.ListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reception>> Handle(GetUpcomingArrivalsQuery query, CancellationToken cancellationToken = default)
    {
        return await receptionRepository.ListUpcomingArrivalsAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reception>> Handle(GetReceptionsBySiteIdQuery query, CancellationToken cancellationToken = default)
    {
        return await receptionRepository.ListBySiteIdAsync(query.SiteId, cancellationToken);
    }
}
