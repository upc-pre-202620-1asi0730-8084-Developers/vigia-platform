namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

public record OrderMainMaterial
{
    public string Value { get; }

    public const int MaxLength = 100;

    public OrderMainMaterial(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Value cannot exceed {MaxLength} characters.", nameof(value));
        }

        Value = normalized;
    }
}