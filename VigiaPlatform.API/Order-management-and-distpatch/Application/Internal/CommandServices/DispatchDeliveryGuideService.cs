using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;

public class DispatchDeliveryGuideService(IDispatchRepository dispatches, IUnitOfWork unitOfWork)
{
    public async Task<Dispatch> FindAsync(int dispatchId, CancellationToken ct) =>
        await dispatches.FindByIdAsync(dispatchId, ct) ?? throw new KeyNotFoundException("Dispatch not found.");

    public async Task<Dispatch> IssueAsync(int dispatchId, string driverName, string carrierName, Guid? requestId,
        int accountId, string actor, CancellationToken ct)
    {
        var driver = DispatchDeliveryGuide.ValidateName(driverName);
        var carrier = DispatchDeliveryGuide.ValidateName(carrierName);
        if (requestId == Guid.Empty) throw new ArgumentException("The request ID cannot be empty.");
        var dispatch = await FindAsync(dispatchId, ct);
        if (requestId.HasValue && dispatch.DeliveryGuide is { } existing && existing.RequestId == requestId.Value.ToString("D"))
        {
            if (existing.DriverName != driver || existing.CarrierName != carrier)
                throw new InvalidOperationException("The request ID was already used with different carrier details.");
            return dispatch;
        }
        dispatch.IssueDeliveryGuide(driver, carrier, accountId, actor, DateTimeOffset.UtcNow, requestId);
        await unitOfWork.CompleteAsync(ct);
        return dispatch;
    }
}
