namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

/// <summary>
/// Lifecycle states of a material reception at the construction site,
/// adhering strictly to the On-Site Receipt and Verification EventStorming flow.
/// </summary>
public enum ReceptionStatus
{
    /// <summary>The transport unit arrival has been registered by the Site Resident.</summary>
    UnitArrived,

    /// <summary>Delivery note details and scale weights have been confirmed at the gate.</summary>
    ReceiptConfirmed,

    /// <summary>Material items have been physical verified and quantities reconciled against the dispatch.</summary>
    MaterialVerified,

    /// <summary>Compliance recorded; quantities and quality conformant; dispatch closed.</summary>
    ConformantClosed,

    /// <summary>A discrepancy (shortage, damage, seal breach) has been formally reported.</summary>
    DiscrepancyReported,

    /// <summary>The reception was rejected due to critical non-conformity.</summary>
    Rejected
}
