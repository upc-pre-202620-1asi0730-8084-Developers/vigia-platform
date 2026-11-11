namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

// Referencia interna (ej. "REF-023").
public record InternalReference
{
    public string Value { get; }

    public InternalReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}
