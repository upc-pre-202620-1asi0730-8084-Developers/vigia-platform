namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

// El site id viene como código de texto (ej. "OB-001"), no como entero.
public record DispatchSiteId
{
    public string Value { get; }

    public DispatchSiteId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}
