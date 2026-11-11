using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;

public interface IDispatchRepository : IBaseRepository<Dispatch>
{
    Task<IEnumerable<Dispatch>> FindByStatusAsync(DispatchStatus status,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Dispatch>> FindByDepartureDateAsync(DispatchApertureDate departureDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Dispatch>> FindByRelatedOrderIdAsync(RelatedOrderId relatedOrderId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByInternalReferenceAsync(InternalReference internalReference,
        CancellationToken cancellationToken = default);
}
