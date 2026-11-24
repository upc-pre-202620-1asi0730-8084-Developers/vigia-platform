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
        if (string.IsNullOrWhiteSpace(value) || value.Trim().ToUpperInvariant().Length > 64 || value.Any(char.IsControl))
            throw new FleetException(FleetErrorCodes.InvalidGpsDevice, "GPS device ID must contain 1 to 64 characters without control characters.");

        Value = value.Trim().ToUpperInvariant();
    }

    public static implicit operator string(GpsDeviceId id) => id.Value;

    public override string ToString() => Value;
}
