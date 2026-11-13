namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Timeline;

public record TimelineEventUser
{
    public string Value { get; }

    public TimelineEventUser(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
