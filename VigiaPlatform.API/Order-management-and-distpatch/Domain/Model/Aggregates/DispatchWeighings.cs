using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;

public partial class Dispatch
{
    private readonly List<DispatchWeighing> _weighings = [];
    public IReadOnlyCollection<DispatchWeighing> Weighings => _weighings.AsReadOnly();
    public long Version { get; private set; }
    public DispatchWeighing? LatestWeighing => _weighings.OrderByDescending(w => w.Id).FirstOrDefault();
    public bool RequiresWeightReview => LatestWeighing is null || LatestWeighing.RequiresReview || !IsCurrent(LatestWeighing);

    public bool IsCurrent(DispatchWeighing weighing) => weighing.CargoFingerprint == CurrentCargoFingerprint();

    private string CurrentCargoFingerprint()
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            vehicleId = VehicleId?.Value, plate = VehiclePlate?.Value,
            items = _items.OrderBy(i => i.Id).Select(i => new
            { i.Id, code = i.Code.Value, material = i.Material.Value, quantity = i.Quantity.Value, unit = i.Unit.ToString() })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
    }

    public DispatchWeighing RecordNetWeight(decimal weightKg, decimal? expectedWeightKg, decimal? toleranceKg,
        int accountId, string actor, DateTimeOffset now, Guid? requestId)
    {
        if (Status != DispatchStatus.InPreparation || VehicleId is null || VehiclePlate is null)
            throw new InvalidOperationException("Weighing requires an IN_PREPARATION dispatch with an assigned vehicle.");
        var weighing = new DispatchWeighing(weightKg, expectedWeightKg, toleranceKg, VehicleId.Value,
            VehiclePlate.Value, CurrentCargoFingerprint(), accountId, actor, now, requestId);
        _weighings.Add(weighing);
        Version++;
        var comparison = weighing.ComparisonStatus switch
        {
            WeightComparisonStatus.NotEvaluated => "Review required: expected weight and tolerance not provided.",
            WeightComparisonStatus.OutsideTolerance => "Review required: net weight outside the supplied tolerance.",
            _ => "Net weight within the supplied tolerance."
        };
        AddTimelineEvent("Net weighing recorded", actor,
            $"{weightKg.ToString("0.###", CultureInfo.InvariantCulture)} kg. {comparison}", now.UtcDateTime);
        return weighing;
    }
}
