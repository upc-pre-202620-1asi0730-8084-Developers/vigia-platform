namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record RecordDispatchWeighingCommand(int DispatchId, decimal WeightKg, decimal? ExpectedWeightKg,
    decimal? ToleranceKg, Guid? RequestId, int AccountId, string Actor);
