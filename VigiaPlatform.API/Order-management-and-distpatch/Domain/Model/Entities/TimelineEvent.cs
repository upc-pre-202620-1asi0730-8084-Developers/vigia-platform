using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Timeline;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

/// <summary>
///     Entry of the dispatch history (created, in transit, completed, ...).
/// </summary>
public class TimelineEvent
{
    public int Id { get; private set; }
    public int DispatchId { get; private set; }
    public TimelineEventName Event { get; private set; } = null!;
    public TimelineEventUser User { get; private set; } = null!;
    public TimelineEventDescription Description { get; private set; } = null!;
    public TimelineEventDate Date { get; private set; } = null!;

    // Required by EF Core
    protected TimelineEvent()
    {
    }

    public TimelineEvent(string eventName, string user, string description, DateTime date)
    {
        Event = new TimelineEventName(eventName);
        User = new TimelineEventUser(user);
        Description = new TimelineEventDescription(description);
        Date = new TimelineEventDate(date);
    }
}
