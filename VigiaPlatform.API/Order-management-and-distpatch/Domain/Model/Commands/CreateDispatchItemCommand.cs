using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Items;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record CreateDispatchItemCommand(
    string Code,
    string Material,
    int Quantity,
    UnitType Unit,
    string? Observation);
