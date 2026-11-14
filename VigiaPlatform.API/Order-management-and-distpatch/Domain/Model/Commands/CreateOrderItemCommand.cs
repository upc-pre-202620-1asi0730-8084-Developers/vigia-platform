using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record CreateOrderItemCommand(string Material, int Quantity, ItemsUnitType Unit, string? Observation);
