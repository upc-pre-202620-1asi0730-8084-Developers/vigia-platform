using VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.QueryServices;

public class DispatchQueryService(IDispatchRepository dispatchRepository) : IDispatchQueryService
{
    public async Task<IEnumerable<Dispatch>> Handle(GetAllDispatchesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await dispatchRepository.ListAsync(cancellationToken);
    }

    public async Task<Dispatch?> Handle(GetDispatchByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await dispatchRepository.FindByIdAsync(query.DispatchId, cancellationToken);
    }

    public async Task<IEnumerable<Dispatch>> Handle(GetDispatchesByStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        return await dispatchRepository.FindByStatusAsync(query.Status, cancellationToken);
    }

    public async Task<IEnumerable<Dispatch>> Handle(GetDispatchesByDepartureDateQuery query,
        CancellationToken cancellationToken = default)
    {
        return await dispatchRepository.FindByDepartureDateAsync(new DispatchApertureDate(query.DepartureDate),
            cancellationToken);
    }

    public async Task<IEnumerable<Dispatch>> Handle(GetDispatchesByRelatedOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        return await dispatchRepository.FindByRelatedOrderIdAsync(new RelatedOrderId(query.RelatedOrderId),
            cancellationToken);
    }
}
