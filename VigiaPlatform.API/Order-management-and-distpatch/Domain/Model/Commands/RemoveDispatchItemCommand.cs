namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record RemoveDispatchItemCommand(int DispatchId, int ItemId, string PerformedBy);
