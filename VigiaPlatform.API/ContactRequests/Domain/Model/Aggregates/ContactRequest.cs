namespace VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates;

public sealed class ContactRequest
{
    public const string StatusReceived = "RECEIVED";
    public static readonly IReadOnlySet<string> AllowedSegments = new HashSet<string>(StringComparer.Ordinal)
    {
        "CONSTRUCTION",
        "LOGISTICS",
        "OTHER"
    };

    public static readonly IReadOnlySet<string> AllowedPlans = new HashSet<string>(StringComparer.Ordinal)
    {
        "STARTER",
        "PRO",
        "ENTERPRISE",
        "CUSTOM"
    };

    public long Id { get; private set; }
    public string? RequestId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string Segment { get; private set; } = "OTHER";
    public string? Plan { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string Locale { get; private set; } = "es";
    public string Status { get; private set; } = StatusReceived;
    public DateTime CreatedAtUtc { get; private set; }

    private ContactRequest() { }

    public ContactRequest(
        string? requestId,
        string fullName,
        string companyName,
        string email,
        string? phone,
        string segment,
        string? plan,
        string message,
        string locale,
        DateTime createdAtUtc)
    {
        RequestId = requestId;
        FullName = fullName;
        CompanyName = companyName;
        Email = email;
        Phone = phone;
        Segment = segment;
        Plan = plan;
        Message = message;
        Locale = locale;
        Status = StatusReceived;
        CreatedAtUtc = createdAtUtc;
    }

    public bool MatchesPayload(
        string fullName,
        string companyName,
        string email,
        string? phone,
        string segment,
        string? plan,
        string message) =>
        string.Equals(FullName, fullName, StringComparison.Ordinal)
        && string.Equals(CompanyName, companyName, StringComparison.Ordinal)
        && string.Equals(Email, email, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Phone ?? string.Empty, phone ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(Segment, segment, StringComparison.Ordinal)
        && string.Equals(Plan ?? string.Empty, plan ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(Message, message, StringComparison.Ordinal);
}
