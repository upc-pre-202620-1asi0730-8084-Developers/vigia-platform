namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

public enum WeightComparisonStatus { NotEvaluated, WithinTolerance, OutsideTolerance }

public class DispatchWeighing
{
    public const decimal MaximumKg = 999999999.999m;
    public int Id { get; private set; }
    public int DispatchId { get; private set; }
    public decimal WeightKg { get; private set; }
    public decimal? ExpectedWeightKg { get; private set; }
    public decimal? ToleranceKg { get; private set; }
    public WeightComparisonStatus ComparisonStatus { get; private set; }
    public int VehicleId { get; private set; }
    public string VehiclePlate { get; private set; } = "";
    public string CargoFingerprint { get; private set; } = "";
    public DateTimeOffset WeighedAt { get; private set; }
    public int RecordedByAccountId { get; private set; }
    public string RecordedBy { get; private set; } = "";
    public string? RequestId { get; private set; }
    public decimal? DifferenceKg => ExpectedWeightKg.HasValue ? WeightKg - ExpectedWeightKg.Value : null;
    public bool RequiresReview => ComparisonStatus != WeightComparisonStatus.WithinTolerance;

    private DispatchWeighing() { }

    public DispatchWeighing(decimal weightKg, decimal? expectedWeightKg, decimal? toleranceKg,
        int vehicleId, string vehiclePlate, string cargoFingerprint, int accountId, string actor,
        DateTimeOffset weighedAt, Guid? requestId)
    {
        ValidateKg(weightKg, false);
        if (expectedWeightKg.HasValue != toleranceKg.HasValue)
            throw new ArgumentException("Provide both expectedWeightKg and toleranceKg, or neither.");
        if (expectedWeightKg.HasValue) ValidateKg(expectedWeightKg.Value, false);
        if (toleranceKg.HasValue) ValidateKg(toleranceKg.Value, true);
        if (accountId <= 0 || vehicleId <= 0 || string.IsNullOrWhiteSpace(actor) || actor.Length > 150 ||
            requestId == Guid.Empty) throw new ArgumentException("A valid actor, vehicle and request ID are required.");
        WeightKg = weightKg;
        ExpectedWeightKg = expectedWeightKg;
        ToleranceKg = toleranceKg;
        ComparisonStatus = !expectedWeightKg.HasValue ? WeightComparisonStatus.NotEvaluated :
            Math.Abs(weightKg - expectedWeightKg.Value) <= toleranceKg!.Value
                ? WeightComparisonStatus.WithinTolerance : WeightComparisonStatus.OutsideTolerance;
        VehicleId = vehicleId;
        VehiclePlate = vehiclePlate;
        CargoFingerprint = cargoFingerprint;
        RecordedByAccountId = accountId;
        RecordedBy = actor;
        WeighedAt = weighedAt.ToUniversalTime();
        RequestId = requestId?.ToString("D");
    }

    private static void ValidateKg(decimal value, bool allowZero)
    {
        if ((allowZero ? value < 0 : value <= 0) || value > MaximumKg || decimal.Round(value, 3) != value)
            throw new ArgumentException("Weights must be positive kg values with at most three decimals; tolerance may be zero.");
    }
}
