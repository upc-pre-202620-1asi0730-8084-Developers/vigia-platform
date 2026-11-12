namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

// Unit: t, kg, u or pcs
public record CreateOrderItemResource(string Material, int Quantity, string Unit, string? Observation);
