using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;

/// <summary>
///     Dispatch aggregate root: materials sent from the warehouse to a work site.
/// </summary>
public partial class Dispatch
{
    private readonly List<DispatchItem> _items = [];
    private readonly List<TimelineEvent> _timeline = [];

    public int Id { get; private set; }
    public DispatchProjectName ProjectName { get; private set; } = null!;
    public DispatchSiteId SiteId { get; private set; } = null!;
    public DispatchAddress Address { get; private set; } = null!;
    public DispatchApertureDate DepartureDate { get; private set; } = null!;
    public DispatchEstimatedTime EstimatedTime { get; private set; } = null!;
    public RelatedOrderId RelatedOrderId { get; private set; } = null!;
    public DispatchType DispatchType { get; private set; }
    public DispatchPriority Priority { get; private set; }
    public DispatchStatus Status { get; private set; }
    public DispatchCreatedBy CreatedBy { get; private set; } = null!;
    public DispatchCreatedAt CreationDate { get; private set; } = null!;
    public InternalReference InternalReference { get; private set; } = null!;
    public DispatchObservations Observations { get; private set; } = null!;
    public VehicleId? VehicleId { get; private set; }
    public VehiclePlate? VehiclePlate { get; private set; }

    public IReadOnlyCollection<DispatchItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<TimelineEvent> Timeline => _timeline.AsReadOnly();

    // Required by EF Core
    protected Dispatch()
    {
    }

    public Dispatch(CreateDispatchCommand command, DateTime createdAt)
    {
        if (command.Items is null || command.Items.Count == 0)
            throw new ArgumentException("A dispatch must contain at least one item", nameof(command));

        ProjectName = new DispatchProjectName(command.ProjectName);
        SiteId = new DispatchSiteId(command.SiteId);
        Address = new DispatchAddress(command.Address);
        DepartureDate = new DispatchApertureDate(command.DepartureDate);
        EstimatedTime = new DispatchEstimatedTime(command.EstimatedTime);
        RelatedOrderId = new RelatedOrderId(command.RelatedOrderId);
        DispatchType = command.DispatchType;
        Priority = command.Priority;
        Status = DispatchStatus.Scheduled;
        CreatedBy = new DispatchCreatedBy(command.CreatedBy);
        CreationDate = new DispatchCreatedAt(createdAt);
        InternalReference = new InternalReference(command.InternalReference);
        Observations = new DispatchObservations(command.Observations);

        foreach (var item in command.Items)
            _items.Add(new DispatchItem(item.Code, item.Material, item.Quantity, item.Unit, item.Observation));

        AddTimelineEvent("Dispatch created", command.CreatedBy, "Dispatch has been created.", createdAt);
    }

    public void AddItem(AddDispatchItemCommand command, DateTime occurredAt)
    {
        EnsureEditable();

        var item = new DispatchItem(command.Code, command.Material, command.Quantity, command.Unit,
            command.Observation);
        _items.Add(item);

        AddTimelineEvent("Item added", command.PerformedBy,
            $"Item {item.Code.Value} ({item.Material.Value}) added to the dispatch.", occurredAt);
    }

    public void RemoveItem(int itemId, string performedBy, DateTime occurredAt)
    {
        EnsureEditable();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
                   ?? throw new InvalidOperationException($"Item {itemId} does not belong to this dispatch.");

        if (_items.Count == 1)
            throw new InvalidOperationException("A dispatch must contain at least one item.");

        _items.Remove(item);

        AddTimelineEvent("Item removed", performedBy,
            $"Item {item.Code.Value} ({item.Material.Value}) removed from the dispatch.", occurredAt);
    }

    public void AssignVehicle(int vehicleId, string vehiclePlate, string performedBy, DateTime occurredAt)
    {
        EnsureEditable();

        if (vehicleId <= 0)
            throw new ArgumentException("Vehicle id must be greater than zero", nameof(vehicleId));

        var plate = new VehiclePlate(vehiclePlate);
        VehicleId = new VehicleId(vehicleId);
        VehiclePlate = plate;

        AddTimelineEvent("Vehicle assigned", performedBy, $"Vehicle {plate.Value} assigned to the dispatch.",
            occurredAt);
    }

    public void ChangeStatus(DispatchStatus newStatus, string performedBy, string? description,
        DateTime occurredAt)
    {
        if (newStatus == DispatchStatus.Cancelled)
            throw new InvalidOperationException("Use Cancel to cancel a dispatch.");

        if (!CanMoveTo(newStatus))
            throw new InvalidOperationException($"Cannot change the dispatch status from {Status} to {newStatus}.");

        if (newStatus == DispatchStatus.InTransit && VehicleId is null)
            throw new InvalidOperationException("A vehicle must be assigned before the dispatch leaves.");

        Status = newStatus;

        AddTimelineEvent(StatusEventName(newStatus), performedBy,
            string.IsNullOrWhiteSpace(description) ? StatusEventDescription(newStatus) : description,
            occurredAt);
    }

    public void Cancel(string reason, string performedBy, DateTime occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (Status is DispatchStatus.InTransit or DispatchStatus.Completed or DispatchStatus.Cancelled)
            throw new InvalidOperationException($"A dispatch with status {Status} cannot be cancelled.");

        Status = DispatchStatus.Cancelled;

        AddTimelineEvent("Dispatch cancelled", performedBy, reason, occurredAt);
    }

    private bool CanMoveTo(DispatchStatus next) => (Status, next) switch
    {
        (DispatchStatus.Scheduled, DispatchStatus.InPreparation) => true,
        (DispatchStatus.InPreparation, DispatchStatus.InTransit) => true,
        (DispatchStatus.InTransit, DispatchStatus.Completed) => true,
        _ => false
    };

    private void EnsureEditable()
    {
        if (Status is not (DispatchStatus.Scheduled or DispatchStatus.InPreparation))
            throw new InvalidOperationException($"A dispatch with status {Status} can no longer be modified.");
    }

    private void AddTimelineEvent(string eventName, string user, string description, DateTime date) =>
        _timeline.Add(new TimelineEvent(eventName, user, description, date));

    private static string StatusEventName(DispatchStatus status) => status switch
    {
        DispatchStatus.Scheduled => "Scheduled",
        DispatchStatus.InPreparation => "In preparation",
        DispatchStatus.InTransit => "In transit",
        DispatchStatus.Completed => "Completed",
        _ => status.ToString()
    };

    private static string StatusEventDescription(DispatchStatus status) => status switch
    {
        DispatchStatus.InPreparation => "The dispatch is being prepared.",
        DispatchStatus.InTransit => "The dispatch left the warehouse.",
        DispatchStatus.Completed => "The dispatch was delivered.",
        _ => $"The dispatch status changed to {status}."
    };
}
