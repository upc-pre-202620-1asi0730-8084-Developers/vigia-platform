using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

public readonly record struct OrderId(int Value) : IEntityId<int>
{
    public static implicit operator int(OrderId id) => id.Value;
    public static implicit operator OrderId(int value) => new(value);

    public override string ToString() => Value.ToString();
}
