using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;

public partial class Dispatch
{
    public DispatchDeliveryGuide? DeliveryGuide { get; private set; }
    public bool IsAuthorized => DeliveryGuide is not null && Status != DispatchStatus.Cancelled &&
        !RequiresWeightReview && DeliveryGuide.WeighingId == LatestWeighing?.Id &&
        DeliveryGuide.CargoFingerprint == CurrentCargoFingerprint();
    public bool CanDepart => Status == DispatchStatus.InPreparation && IsAuthorized;

    public DispatchDeliveryGuide IssueDeliveryGuide(string driverName, string carrierName, int accountId,
        string actor, DateTimeOffset now, Guid? requestId)
    {
        if (DeliveryGuide is not null)
            throw new InvalidOperationException("A guide has already been issued for this dispatch.");
        if (Status != DispatchStatus.InPreparation || VehicleId is null || RequiresWeightReview)
            throw new InvalidOperationException("Guide issuance requires an IN_PREPARATION dispatch, vehicle and current weighing within tolerance.");
        DeliveryGuide = new DispatchDeliveryGuide(Id, ProjectName.Value, SiteId.Value, Address.Value,
            RelatedOrderId.Value, driverName, carrierName, LatestWeighing!,
            Items.Select(item => new DeliveryGuideItem(item.Id, item.Code.Value, item.Material.Value,
                item.Quantity.Value, item.Unit.ToString())).ToArray(), accountId, actor, now, requestId);
        Version++;
        AddTimelineEvent("Delivery guide issued", actor,
            $"Guide {DeliveryGuide.GuideNumber} issued; departure authorized.", now.UtcDateTime);
        return DeliveryGuide;
    }
}
