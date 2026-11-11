using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public static class DispatchCommandFromResourceAssembler
{
    public static AddDispatchItemCommand ToCommandFromResource(int dispatchId, AddDispatchItemResource resource) =>
        new(dispatchId,
            resource.Code,
            resource.Material,
            resource.Quantity,
            EnumApiFormat.FromApi<UnitType>(resource.Unit, nameof(resource.Unit)),
            resource.Observation,
            resource.PerformedBy);

    public static RemoveDispatchItemCommand ToCommandFromResource(int dispatchId, int itemId,
        RemoveDispatchItemResource resource) =>
        new(dispatchId, itemId, resource.PerformedBy);

    public static AssignVehicleCommand ToCommandFromResource(int dispatchId, AssignVehicleResource resource) =>
        new(dispatchId, resource.VehicleId, resource.VehiclePlate, resource.PerformedBy);

    public static ChangeDispatchStatusCommand ToCommandFromResource(int dispatchId,
        ChangeDispatchStatusResource resource) =>
        new(dispatchId,
            EnumApiFormat.FromApi<DispatchStatus>(resource.Status, nameof(resource.Status)),
            resource.PerformedBy,
            resource.Description);

    public static CancelDispatchCommand ToCommandFromResource(int dispatchId, CancelDispatchResource resource) =>
        new(dispatchId, resource.Reason, resource.PerformedBy);
}
