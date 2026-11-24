using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

namespace VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;

public class DispatchWeighingService(IDispatchRepository dispatches, IUnitOfWork unitOfWork)
{
    public async Task<Dispatch> FindAsync(int dispatchId, CancellationToken ct) =>
        await dispatches.FindByIdAsync(dispatchId, ct) ?? throw new KeyNotFoundException("Dispatch not found.");

    public async Task<(Dispatch Dispatch, DispatchWeighing Weighing)> RecordAsync(RecordDispatchWeighingCommand command,
        CancellationToken ct)
    {
        var dispatch = await FindAsync(command.DispatchId, ct);
        if (command.RequestId.HasValue)
        {
            var existing = dispatch.Weighings.SingleOrDefault(w => w.RequestId == command.RequestId.Value.ToString("D"));
            if (existing is not null)
            {
                if (existing.WeightKg != command.WeightKg || existing.ExpectedWeightKg != command.ExpectedWeightKg ||
                    existing.ToleranceKg != command.ToleranceKg)
                    throw new InvalidOperationException("The request ID was already used with different weighing values.");
                return (dispatch, existing);
            }
        }
        var weighing = dispatch.RecordNetWeight(command.WeightKg, command.ExpectedWeightKg, command.ToleranceKg,
            command.AccountId, command.Actor, DateTimeOffset.UtcNow, command.RequestId);
        await unitOfWork.CompleteAsync(ct);
        return (dispatch, weighing);
    }
}
