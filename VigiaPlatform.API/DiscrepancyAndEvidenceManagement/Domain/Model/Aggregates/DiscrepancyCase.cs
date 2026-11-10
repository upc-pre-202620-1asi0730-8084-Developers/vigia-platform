namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;

public class DiscrepancyCase
{
    public Guid Id { get; private set; }
    public Guid DispatchId { get; private set; }
    public Guid ReceptionId { get; private set; }
    public string CaseNumber { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string Status { get; private set; }
    public string DeterminedLiability { get; private set; }
    public string? ResolutionSummary { get; private set; }

    private DiscrepancyCase()
    {
        CaseNumber = string.Empty;
        Status = string.Empty;
        DeterminedLiability = string.Empty;
    }

    public DiscrepancyCase(
        Guid dispatchId,
        Guid receptionId,
        string caseNumber)
    {
        if (dispatchId == Guid.Empty)
            throw new ArgumentException("Dispatch ID is required.");

        if (receptionId == Guid.Empty)
            throw new ArgumentException("Reception ID is required.");

        ArgumentException.ThrowIfNullOrWhiteSpace(caseNumber);

        Id = Guid.NewGuid();
        DispatchId = dispatchId;
        ReceptionId = receptionId;
        CaseNumber = caseNumber;
        OpenedAt = DateTimeOffset.UtcNow;
        Status = "Open";
        DeterminedLiability = "Pending";
    }
}