namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public record TimelineEventName
{
    public string Value { get; }

    public TimelineEventName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > 100)
            throw new ArgumentException("Event name cannot be longer than 100 characters", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
