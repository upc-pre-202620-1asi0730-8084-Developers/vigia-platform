namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

// Referencia a la solicitud/orden relacionada (ej. "SOL-778").
public record RelatedOrderId
{
    public string Value { get; }

    public RelatedOrderId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}
