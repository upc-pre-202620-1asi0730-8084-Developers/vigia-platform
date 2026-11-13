using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;

/// <summary>
///     Order aggregate root: a request of materials made by a project requester.
/// </summary>
public partial class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderAttachedFile> _attachedFiles = [];
    private readonly List<OrderTimelineEvent> _timeline = [];

    public int Id { get; private set; }
    public OrderCode Code { get; private set; } = null!;
    public ProjectName ProjectName { get; private set; } = null!;
    public OrderRequesterName RequesterName { get; private set; } = null!;
    public OrderStatusType Status { get; private set; }
    public OrderPriorityType Priority { get; private set; }
    public OrderDate Date { get; private set; } = null!;
    public OrderMainMaterial MainMaterial { get; private set; } = null!;
    public OrderObservation Observations { get; private set; } = null!;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderAttachedFile> AttachedFiles => _attachedFiles.AsReadOnly();
    public IReadOnlyCollection<OrderTimelineEvent> Timeline => _timeline.AsReadOnly();

    // Required by EF Core
    protected Order()
    {
    }

    public Order(CreateOrderCommand command, DateTime createdAt)
    {
        if (command.Items is null || command.Items.Count == 0)
            throw new ArgumentException("An order must contain at least one item", nameof(command));

        Code = new OrderCode(command.Code);
        ProjectName = new ProjectName(command.ProjectName);
        RequesterName = new OrderRequesterName(command.RequesterName);
        Status = OrderStatusType.Pending;
        Priority = command.Priority;
        Date = new OrderDate(command.Date.ToDateTime(TimeOnly.MinValue));
        MainMaterial = new OrderMainMaterial(command.MainMaterial);
        Observations = new OrderObservation(command.Observations);

        foreach (var item in command.Items)
            _items.Add(new OrderItem(item.Material, item.Quantity, item.Unit, item.Observation));

        foreach (var file in command.AttachedFiles ?? [])
            _attachedFiles.Add(new OrderAttachedFile(file.Name, file.Type, file.SizeInBytes));

        AddTimelineEvent("Order created", command.RequesterName,
            $"Order {Code.Value} has been created for {ProjectName.Value}.", createdAt);
    }

    public void AddAttachedFile(string name, AttachedFileType type, long sizeInBytes, string performedBy,
        DateTime occurredAt)
    {
        EnsureEditable();

        var file = new OrderAttachedFile(name, type, sizeInBytes);
        _attachedFiles.Add(file);

        AddTimelineEvent("File attached", performedBy, $"File {file.Name.Value} attached to the order.", occurredAt);
    }

    public void ChangeStatus(OrderStatusType newStatus, string performedBy, string? description, DateTime occurredAt)
    {
        if (!CanMoveTo(newStatus))
            throw new InvalidOperationException($"Cannot change the order status from {Status} to {newStatus}.");

        if (newStatus == OrderStatusType.Rejected && string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A reason is required to reject an order.", nameof(description));

        Status = newStatus;

        AddTimelineEvent(StatusEventTitle(newStatus), performedBy,
            string.IsNullOrWhiteSpace(description) ? StatusEventDescription(newStatus) : description,
            occurredAt);
    }

    private bool CanMoveTo(OrderStatusType next) => (Status, next) switch
    {
        (OrderStatusType.Pending, OrderStatusType.UnderReview) => true,
        (OrderStatusType.UnderReview, OrderStatusType.Approved) => true,
        (OrderStatusType.UnderReview, OrderStatusType.Rejected) => true,
        _ => false
    };

    private void EnsureEditable()
    {
        if (Status is not (OrderStatusType.Pending or OrderStatusType.UnderReview))
            throw new InvalidOperationException($"An order with status {Status} can no longer be modified.");
    }

    private void AddTimelineEvent(string title, string user, string description, DateTime date) =>
        _timeline.Add(new OrderTimelineEvent(title, user, description, date));

    private static string StatusEventTitle(OrderStatusType status) => status switch
    {
        OrderStatusType.UnderReview => "Under review",
        OrderStatusType.Approved => "Approved",
        OrderStatusType.Rejected => "Rejected",
        _ => status.ToString()
    };

    private static string StatusEventDescription(OrderStatusType status) => status switch
    {
        OrderStatusType.UnderReview => "The order is under review by the warehouse area.",
        OrderStatusType.Approved => "The order was approved.",
        OrderStatusType.Rejected => "The order was rejected.",
        _ => $"The order status changed to {status}."
    };
}
