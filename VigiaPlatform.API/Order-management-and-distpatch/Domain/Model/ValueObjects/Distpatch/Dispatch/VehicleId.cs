using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

// En el agregado se usa como VehicleId? porque un despacho puede no tener vehículo asignado.
public readonly record struct VehicleId(int Value) : IEntityId<int>
{
    public static implicit operator int(VehicleId id) => id.Value;
    public static implicit operator VehicleId(int value) => new(value);

    public override string ToString() => Value.ToString();
}
