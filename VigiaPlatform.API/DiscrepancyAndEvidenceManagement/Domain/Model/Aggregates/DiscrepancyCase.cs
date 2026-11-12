namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;

public class DiscrepancyCase
{
    public int Id { get; private set; }
    public string DispatchId { get; private set; }
    public string ReceptionId { get; private set; }
    public string CaseNumber { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string Status { get; private set; }
    public string DeterminedLiability { get; private set; }
    public string? ResolutionSummary { get; private set; }

    private DiscrepancyCase()
    {
        DispatchId = string.Empty;
        ReceptionId = string.Empty;
        CaseNumber = string.Empty;
        Status = string.Empty;
        DeterminedLiability = string.Empty;
    }

    public DiscrepancyCase(
        string dispatchId,
        string receptionId,
        string caseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(receptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseNumber);

        DispatchId = dispatchId.Trim();
        ReceptionId = receptionId.Trim();
        CaseNumber = caseNumber.Trim();
        OpenedAt = DateTimeOffset.UtcNow;
        Status = "Open";
        DeterminedLiability = "Pending";
    }

    public void DetermineResponsibility(string responsibility)
    {
        if (Status == "Closed")
        {
            throw new InvalidOperationException(
                "Cannot change the responsibility of a closed case.");
        }

        if (responsibility != "Warehouse" &&
            responsibility != "Transport" &&
            responsibility != "Site")
        {
            throw new ArgumentException(
                "Invalid responsibility.");
        }

        DeterminedLiability = responsibility;
    }

    public void Close(string? resolutionSummary)
    {
        if (Status == "Closed")
        {
            throw new InvalidOperationException(
                "The discrepancy case is already closed.");
        }

        if (DeterminedLiability == "Pending")
        {
            throw new InvalidOperationException(
                "Responsibility must be determined before closing the case.");
        }

        if (resolutionSummary?.Length > 1000)
        {
            throw new ArgumentException(
                "Resolution summary cannot exceed 1000 characters.");
        }

        ResolutionSummary = string.IsNullOrWhiteSpace(resolutionSummary)
            ? null
            : resolutionSummary.Trim();

        Status = "Closed";
        ClosedAt = DateTimeOffset.UtcNow;
    }
}
