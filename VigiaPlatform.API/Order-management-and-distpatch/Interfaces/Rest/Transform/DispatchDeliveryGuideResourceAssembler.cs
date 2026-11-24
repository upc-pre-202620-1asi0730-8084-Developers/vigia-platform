using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class DispatchDeliveryGuideResourceAssembler
{
    public static DispatchDeliveryGuideResource? ToResource(Dispatch dispatch)
    {
        var guide = dispatch.DeliveryGuide;
        return guide is null ? null : new(guide.DispatchId, guide.GuideNumber, guide.ProjectName, guide.SiteId,
            guide.Address, guide.RelatedOrderId, guide.DriverName, guide.CarrierName, guide.VehicleId, guide.VehiclePlate,
            guide.WeighingId, "NET", guide.WeightKg, guide.ExpectedWeightKg, guide.ToleranceKg, guide.Items,
            guide.IssuedAt, guide.IssuedByAccountId, guide.IssuedBy, guide.RequestId, dispatch.IsAuthorized);
    }
}
