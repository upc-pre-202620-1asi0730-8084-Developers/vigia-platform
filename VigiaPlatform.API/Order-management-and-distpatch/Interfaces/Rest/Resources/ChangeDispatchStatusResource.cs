namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record ChangeDispatchStatusResource(string Status, string PerformedBy, string? Description);
