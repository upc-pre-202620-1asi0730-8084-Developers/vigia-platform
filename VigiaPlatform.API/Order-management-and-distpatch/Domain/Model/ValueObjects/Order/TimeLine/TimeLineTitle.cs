namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

public record TimeLineTitle
{
    public string Value { get; }

    public const int MaxLength = 100;

    public TimeLineTitle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Title cannot exceed {MaxLength} characters.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
