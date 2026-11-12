namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Items;

public record Material
{
    public string Value { get; }

    public Material(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > 100)
            throw new ArgumentException("Material cannot be longer than 100 characters", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
