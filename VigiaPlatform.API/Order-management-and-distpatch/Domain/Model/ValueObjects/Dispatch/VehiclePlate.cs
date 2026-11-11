namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

// Placa del vehículo (ej. "V3B-920"). En el agregado se usa como VehiclePlate? (puede ser null).
public record VehiclePlate
{
    public string Value { get; }

    public VehiclePlate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}
