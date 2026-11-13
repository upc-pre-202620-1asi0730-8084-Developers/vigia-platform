namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record AddDispatchItemResource(
    string Code,
    string Material,
    int Quantity,
    string Unit,
    string? Observation,
    string PerformedBy);
