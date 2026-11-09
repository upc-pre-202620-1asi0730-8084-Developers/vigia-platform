namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Standard error codes for the Site Reception and Verification Bounded Context.
/// </summary>
public static class SiteReceptionErrorCodes
{
    public const string InvalidArrival = "SITE_RECEPTION_INVALID_ARRIVAL";
    public const string InvalidWeight = "SITE_RECEPTION_INVALID_WEIGHT";
    public const string InvalidSignature = "SITE_RECEPTION_INVALID_SIGNATURE";
    public const string InvalidDiscrepancy = "SITE_RECEPTION_INVALID_DISCREPANCY";
    public const string InvalidEvidence = "SITE_RECEPTION_INVALID_EVIDENCE";
    public const string InvalidStateTransition = "SITE_RECEPTION_INVALID_STATE_TRANSITION";
    public const string ItemNotFound = "SITE_RECEPTION_ITEM_NOT_FOUND";
    public const string ReceptionAlreadyClosed = "SITE_RECEPTION_ALREADY_CLOSED";
    public const string DiscrepancyAlreadyReported = "SITE_RECEPTION_DISCREPANCY_ALREADY_REPORTED";
}
