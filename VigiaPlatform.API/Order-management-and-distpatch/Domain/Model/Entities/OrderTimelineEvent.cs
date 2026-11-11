using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

/// <summary>Entry of the order history (created, under review, approved, ...).</summary>
public class OrderTimelineEvent
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public TimeLineTitle Title { get; private set; } = null!;
    public TimeLineUser User { get; private set; } = null!;
    public TimeLineDescription Description { get; private set; } = null!;
    public TimeLineDate Date { get; private set; } = null!;

    // Required by EF Core
    protected OrderTimelineEvent()
    {
    }

    public OrderTimelineEvent(string title, string user, string description, DateTime date)
    {
        Title = new TimeLineTitle(title);
        User = new TimeLineUser(user);
        Description = new TimeLineDescription(description);
        Date = new TimeLineDate(date);
    }
}
