namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

// Opcional: puede venir vacía ("").
public record DispatchObservations
{
    public string Value { get; }

    public DispatchObservations(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (normalized.Length > 500)
            throw new ArgumentException("Observations cannot be longer than 500 characters", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
