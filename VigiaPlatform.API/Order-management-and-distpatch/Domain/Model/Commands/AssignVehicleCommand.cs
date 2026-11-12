namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record AssignVehicleCommand(int DispatchId, int VehicleId, string VehiclePlate, string PerformedBy);
