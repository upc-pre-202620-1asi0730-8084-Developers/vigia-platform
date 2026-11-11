namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public record DispatchAddress
{
    public string Value { get; }

    public DispatchAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > 200)
            throw new ArgumentException("Address cannot be longer than 200 characters", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
