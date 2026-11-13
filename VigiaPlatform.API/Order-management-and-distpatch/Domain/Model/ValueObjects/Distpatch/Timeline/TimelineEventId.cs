using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Timeline;

public readonly record struct TimelineEventId(int Value) : IEntityId<int>
{
    public static implicit operator int(TimelineEventId id) => id.Value;
    public static implicit operator TimelineEventId(int value) => new(value);

    public override string ToString() => Value.ToString();
}
