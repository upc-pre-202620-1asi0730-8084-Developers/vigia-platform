namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Domain exception thrown when business invariants of the Site Reception and Verification BC are violated.
/// </summary>
public class SiteReceptionException : Exception
{
    public string Code { get; }

    public SiteReceptionException(string code, string message) : base(message)
    {
        Code = code;
    }

    public SiteReceptionException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}
