using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record ChangeDispatchStatusCommand(
    int DispatchId,
    DispatchStatus NewStatus,
    string PerformedBy,
    string? Description);
