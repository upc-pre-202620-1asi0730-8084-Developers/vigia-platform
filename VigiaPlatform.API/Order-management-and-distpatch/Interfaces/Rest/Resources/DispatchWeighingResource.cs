namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record RecordDispatchWeighingResource(decimal WeightKg, decimal? ExpectedWeightKg = null,
    decimal? ToleranceKg = null, Guid? RequestId = null);

public record DispatchWeighingResource(int Id, int DispatchId, string WeightBasis, decimal WeightKg,
    decimal? ExpectedWeightKg, decimal? ToleranceKg, decimal? DifferenceKg, string ComparisonStatus,
    bool RequiresReview, bool IsCurrent, int VehicleId, string VehiclePlate, DateTimeOffset WeighedAt,
    int RecordedByAccountId, string RecordedBy, string? RequestId);
