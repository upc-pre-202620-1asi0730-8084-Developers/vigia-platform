namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

// Status: UNDER_REVIEW, APPROVED or REJECTED (a reason is required to reject)
public record ChangeOrderStatusResource(string Status, string PerformedBy, string? Description);
