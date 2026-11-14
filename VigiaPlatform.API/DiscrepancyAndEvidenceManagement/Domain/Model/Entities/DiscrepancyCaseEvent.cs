namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;

public class DiscrepancyCaseEvent
{
    public int Id { get; private set; }
    public int DiscrepancyCaseId { get; private set; }
    public string Stage { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private DiscrepancyCaseEvent()
    {
        Stage = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
    }

    public DiscrepancyCaseEvent(
        string stage,
        string title,
        string description,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Stage = stage;
        Title = title;
        Description = description;
        OccurredAt = occurredAt;
    }
}