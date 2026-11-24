namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

using System.Text.RegularExpressions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;

/// <summary>
/// Value Object representing a normalized vehicle license plate.
/// Guarantees uppercase format without whitespace and valid length.
/// </summary>
public partial record LicensePlate
{
    public string Value { get; init; } = string.Empty;

    public LicensePlate() { }

    public LicensePlate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FleetException(FleetErrorCodes.InvalidLicensePlate, "License plate cannot be empty.");

        var normalized = Normalize(value);
        if (normalized.Length < 5 || normalized.Length > 12 || !Regex.IsMatch(normalized, @"^[A-Z0-9]+(?:-[A-Z0-9]+)*$"))
            throw new FleetException(FleetErrorCodes.InvalidLicensePlate,
                $"License plate '{value}' is invalid. Use 5 to 12 letters/digits with optional separating hyphens.");

        Value = normalized;
    }

    public static string Normalize(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
        return Regex.Replace(plate.Trim().ToUpperInvariant(), @"\s+", "");
    }

    public static implicit operator string(LicensePlate plate) => plate.Value;

    public override string ToString() => Value;
}
