namespace VigiaPlatform.API.ActivityHistory.Domain.Model;

public class ActivityRecord
{
    public int Id { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Type { get; private set; } = "";
    public string Action { get; private set; } = "";
    public string Detail { get; private set; } = "";
    public string UserName { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string Status { get; private set; } = "";

    private ActivityRecord() { }

    public ActivityRecord(string type, string action, string detail, string userName, string reference, string status)
    {
        Type = type;
        Action = action;
        Detail = detail;
        UserName = userName;
        Reference = reference;
        Status = status;
        OccurredAt = DateTimeOffset.UtcNow;
    }
}
