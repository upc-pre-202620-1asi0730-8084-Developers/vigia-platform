using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;

public readonly record struct ItemsId(int Value) : IEntityId<int>
{
    public static implicit operator int(ItemsId id) => id.Value;
    public static implicit operator ItemsId(int value) => new(value);

    public override string ToString() => Value.ToString();
}
