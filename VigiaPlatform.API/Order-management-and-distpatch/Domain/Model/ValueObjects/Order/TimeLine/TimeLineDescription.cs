namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

public record TimeLineDescription
{
    public string Value { get; }

    public const int MaxLength = 500;

    public TimeLineDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Description cannot exceed {MaxLength} characters.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
