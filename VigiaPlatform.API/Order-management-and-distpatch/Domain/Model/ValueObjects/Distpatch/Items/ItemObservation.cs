namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Items;

// Opcional: puede venir vacía ("").
public record ItemObservation
{
    public string Value { get; }

    public ItemObservation(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (normalized.Length > 250)
            throw new ArgumentException("Observation cannot be longer than 250 characters", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
