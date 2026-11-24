using System.Text.Json;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

public record DeliveryGuideItem(int ItemId, string Code, string Material, int Quantity, string Unit);

public class DispatchDeliveryGuide
{
    public int DispatchId { get; private set; }
    public string GuideNumber { get; private set; } = "";
    public string ProjectName { get; private set; } = "";
    public string SiteId { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string RelatedOrderId { get; private set; } = "";
    public string DriverName { get; private set; } = "";
    public string CarrierName { get; private set; } = "";
    public int VehicleId { get; private set; }
    public string VehiclePlate { get; private set; } = "";
    public int WeighingId { get; private set; }
    public decimal WeightKg { get; private set; }
    public decimal ExpectedWeightKg { get; private set; }
    public decimal ToleranceKg { get; private set; }
    public string CargoFingerprint { get; private set; } = "";
    public string ItemsJson { get; private set; } = "";
    public DateTimeOffset IssuedAt { get; private set; }
    public int IssuedByAccountId { get; private set; }
    public string IssuedBy { get; private set; } = "";
    public string? RequestId { get; private set; }
    public IReadOnlyList<DeliveryGuideItem> Items => JsonSerializer.Deserialize<DeliveryGuideItem[]>(ItemsJson)!;

    private DispatchDeliveryGuide() { }

    public DispatchDeliveryGuide(int dispatchId, string projectName, string siteId, string address, string relatedOrderId,
        string driverName, string carrierName, DispatchWeighing weighing, IReadOnlyList<DeliveryGuideItem> items,
        int accountId, string actor, DateTimeOffset now, Guid? requestId)
    {
        DriverName = ValidateName(driverName);
        CarrierName = ValidateName(carrierName);
        if (dispatchId <= 0 || accountId <= 0 || string.IsNullOrWhiteSpace(actor) || actor.Length > 150 || requestId == Guid.Empty)
            throw new ArgumentException("A persisted dispatch, authenticated actor and valid request ID are required.");
        if (weighing.Id <= 0 || weighing.DispatchId != dispatchId || weighing.RequiresReview || items.Count == 0)
            throw new InvalidOperationException("The guide requires a persisted conforming weighing and material items.");
        DispatchId = dispatchId;
        GuideNumber = $"GD-{dispatchId:D8}";
        ProjectName = projectName;
        SiteId = siteId;
        Address = address;
        RelatedOrderId = relatedOrderId;
        VehicleId = weighing.VehicleId;
        VehiclePlate = weighing.VehiclePlate;
        WeighingId = weighing.Id;
        WeightKg = weighing.WeightKg;
        ExpectedWeightKg = weighing.ExpectedWeightKg!.Value;
        ToleranceKg = weighing.ToleranceKg!.Value;
        CargoFingerprint = weighing.CargoFingerprint;
        ItemsJson = JsonSerializer.Serialize(items);
        IssuedAt = new DateTimeOffset(now.UtcTicks / 10 * 10, TimeSpan.Zero);
        IssuedByAccountId = accountId;
        IssuedBy = actor;
        RequestId = requestId?.ToString("D");
    }

    public static string ValidateName(string? value)
    {
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 150 || name.Any(char.IsControl))
            throw new ArgumentException("Driver and carrier names are required, with at most 150 characters and no control characters.");
        return name;
    }
}
