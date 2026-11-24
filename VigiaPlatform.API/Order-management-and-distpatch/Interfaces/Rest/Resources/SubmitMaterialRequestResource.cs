namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record SubmitMaterialRequestResource(int ProjectId, string Priority, DateOnly Date,
    IReadOnlyList<RequestedMaterialResource> Items, string? Observations);

public record RequestedMaterialResource(int MaterialId, int Quantity, string? Observation);

public record RequestMaterialResource(int Id, string Name, string Unit);

public record RequestProjectResource(int Id, string Name, IReadOnlyList<RequestMaterialResource> Materials);
