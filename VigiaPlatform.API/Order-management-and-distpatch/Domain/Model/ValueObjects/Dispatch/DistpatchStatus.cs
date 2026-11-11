namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public enum DispatchStatus
{
    Scheduled,
    InPreparation,
    InTransit,
    Completed,
    Cancelled
}
