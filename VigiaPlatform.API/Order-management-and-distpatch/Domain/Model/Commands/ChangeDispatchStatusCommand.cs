using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record ChangeDispatchStatusCommand(
    int DispatchId,
    DispatchStatus NewStatus,
    string PerformedBy,
    string? Description);
