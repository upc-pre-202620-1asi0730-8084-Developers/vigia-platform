namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

/// <summary>Observación opcional de la orden; se permite vacía.</summary>
public record OrderObservation
{
    public string Value { get; }

    public const int MaxLength = 350;

    public OrderObservation(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();

        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Observation cannot be longer than {MaxLength} characters.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
