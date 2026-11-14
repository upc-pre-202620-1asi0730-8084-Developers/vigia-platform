namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;

/// <summary>
/// Value Object representing a unique telematics GPS device hardware identifier.
/// </summary>
public record GpsDeviceId
{
    public string Value { get; init; } = string.Empty;

    public GpsDeviceId() { }

    public GpsDeviceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FleetException(FleetErrorCodes.InvalidGpsDevice, "GPS device ID cannot be empty or whitespace.");

        Value = value.Trim().ToUpperInvariant();
    }

    public static implicit operator string(GpsDeviceId id) => id.Value;

    public override string ToString() => Value;
}
