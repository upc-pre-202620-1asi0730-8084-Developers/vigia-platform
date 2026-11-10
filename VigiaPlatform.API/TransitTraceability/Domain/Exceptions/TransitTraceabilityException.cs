namespace VigiaPlatform.API.TransitTraceability.Domain.Exceptions;

/// <summary>
/// Raised by the domain model when a Transit Traceability invariant is violated.
/// The command services translate it into a <c>Result</c> failure.
/// </summary>
public sealed class TransitTraceabilityException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
