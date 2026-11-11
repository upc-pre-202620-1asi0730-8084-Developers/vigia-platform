using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public readonly record struct ItemId(int Value) : IEntityId<int>
{
    public static implicit operator int(ItemId id) => id.Value;
    public static implicit operator ItemId(int value) => new(value);

    public override string ToString() => Value.ToString();
}