using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class DispatchWeighingResourceAssembler
{
    public static DispatchWeighingResource ToResource(Dispatch dispatch, DispatchWeighing weighing) =>
        new(weighing.Id, dispatch.Id, "NET", weighing.WeightKg, weighing.ExpectedWeightKg, weighing.ToleranceKg,
            weighing.DifferenceKg, EnumApiFormat.ToApi(weighing.ComparisonStatus),
            weighing.RequiresReview || !dispatch.IsCurrent(weighing), dispatch.IsCurrent(weighing),
            weighing.VehicleId, weighing.VehiclePlate, weighing.WeighedAt, weighing.RecordedByAccountId,
            weighing.RecordedBy, weighing.RequestId);
}
