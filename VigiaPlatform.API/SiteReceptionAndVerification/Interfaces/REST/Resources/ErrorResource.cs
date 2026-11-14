namespace VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Resources;

/// <summary>
/// Standard error representation returned when an invariant or domain rule is violated.
/// </summary>
public record ErrorResource(string Code, string Message);
