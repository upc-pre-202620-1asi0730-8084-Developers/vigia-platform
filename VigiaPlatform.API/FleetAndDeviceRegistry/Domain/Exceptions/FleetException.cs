namespace VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;

/// <summary>
/// Domain exception thrown when business invariants or EventStorming rules
/// of the Fleet and Device Registry Bounded Context are violated.
/// </summary>
public class FleetException : Exception
{
    public string Code { get; }

    public FleetException(string code, string message) : base(message)
    {
        Code = code;
    }

    public FleetException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}
