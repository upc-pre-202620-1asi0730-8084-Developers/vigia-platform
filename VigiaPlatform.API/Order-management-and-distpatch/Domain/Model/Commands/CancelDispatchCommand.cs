namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record CancelDispatchCommand(int DispatchId, string Reason, string PerformedBy);
