using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

public readonly record struct TimeLineId(int Value) : IEntityId<int>
{
    public static implicit operator int(TimeLineId id) => id.Value;
    public static implicit operator TimeLineId(int value) => new(value);

    public override string ToString() => Value.ToString();
}