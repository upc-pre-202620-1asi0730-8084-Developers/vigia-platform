using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record IssueDispatchDeliveryGuideResource(string DriverName, string CarrierName, Guid? RequestId = null);
public record DispatchDeliveryGuideResource(int DispatchId, string GuideNumber, string ProjectName, string SiteId,
    string Address, string RelatedOrderId, string DriverName, string CarrierName, int VehicleId, string VehiclePlate,
    int WeighingId, string WeightBasis, decimal WeightKg, decimal ExpectedWeightKg, decimal ToleranceKg,
    IReadOnlyList<DeliveryGuideItem> Items, DateTimeOffset IssuedAt, int IssuedByAccountId, string IssuedBy,
    string? RequestId, bool IsAuthorized);
