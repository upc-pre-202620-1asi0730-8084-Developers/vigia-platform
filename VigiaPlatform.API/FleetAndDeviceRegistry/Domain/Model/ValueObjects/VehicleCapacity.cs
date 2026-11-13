namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;

/// <summary>
/// Value Object representing the maximum payload capacity of a transport unit in metric tons.
/// Invariant: Must be strictly greater than zero and within reasonable heavy transport bounds.
/// </summary>
public record VehicleCapacity
{
    public double Tons { get; init; }

    public VehicleCapacity() { }

    public VehicleCapacity(double tons)
    {
        if (double.IsNaN(tons) || tons <= 0)
            throw new FleetException(FleetErrorCodes.InvalidCapacity,
                $"Vehicle capacity must be greater than zero. Received: {tons} tons.");

        if (tons > 150)
            throw new FleetException(FleetErrorCodes.InvalidCapacity,
                $"Vehicle capacity exceeds maximum allowable heavy vehicle threshold of 150 tons.");

        Tons = Math.Round(tons, 2);
    }

    public static implicit operator double(VehicleCapacity capacity) => capacity.Tons;

    public override string ToString() => $"{Tons} t";
}
