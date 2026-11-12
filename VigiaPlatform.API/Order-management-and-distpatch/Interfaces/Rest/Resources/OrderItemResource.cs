namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record OrderItemResource(int Id, string Material, int Quantity, string Unit, string Observation);
