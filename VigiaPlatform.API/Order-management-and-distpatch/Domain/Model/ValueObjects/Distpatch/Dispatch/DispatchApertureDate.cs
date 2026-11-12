namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

// Fecha de salida del despacho (campo "departureDate" del JSON, formato "yyyy-MM-dd").
public record DispatchApertureDate
{
    public DateOnly Value { get; }

    public DispatchApertureDate(DateOnly value)
    {
        if (value == default)
            throw new ArgumentException("Aperture date is required", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd");
}
