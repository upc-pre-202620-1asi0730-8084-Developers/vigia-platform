namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public record TimelineEventDescription
{
    public string Value { get; }

    public TimelineEventDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > 500)
            throw new ArgumentException("Event description cannot be longer than 500 characters", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
