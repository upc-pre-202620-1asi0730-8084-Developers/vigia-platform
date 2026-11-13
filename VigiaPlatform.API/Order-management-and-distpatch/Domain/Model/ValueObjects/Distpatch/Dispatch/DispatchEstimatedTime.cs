namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

// Hora estimada de salida, formato "HH:mm" (ej. "14:30").
public record DispatchEstimatedTime
{
    public TimeOnly Value { get; }

    public DispatchEstimatedTime(TimeOnly value)
    {
        Value = value;
    }

    public DispatchEstimatedTime(string value)
    {
        if (!TimeOnly.TryParseExact(value?.Trim(), "HH:mm", out var parsed))
            throw new ArgumentException("Estimated time must have the format HH:mm", nameof(value));

        Value = parsed;
    }

    public override string ToString() => Value.ToString("HH:mm");
}
