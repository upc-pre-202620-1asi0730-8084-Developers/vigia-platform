using VigiaPlatform.API.Shared.Domain.Model;
using VigiaPlatform.API.TransitTraceability.Domain.Exceptions;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;

/// <summary>
/// Route aggregate root (trip of a vehicle carrying a dispatch) of the Transit Traceability bounded context.
/// DispatchId references Order Management and Dispatch; VehicleId and DestinationGeofenceId reference
/// Fleet and Device Management. They are plain identifiers: no cross-context navigation.
/// </summary>
/// <remarks>
/// Invariants: positions are only accepted while the route is active and in chronological order, and each
/// deviation or prolonged stop raises a single alert until it is cleared.
/// The class is named TransitRoute (not Route) to avoid clashing with Microsoft.AspNetCore.Routing.Route.
/// </remarks>
public class TransitRoute : IAuditableEntity
{
    /// <summary>Radius in meters within which consecutive positions count as the vehicle standing still.</summary>
    private const double StopRadiusMeters = 50;

    private readonly List<RouteWaypoint> _waypoints = [];
    private readonly List<TelemetryPosition> _positions = [];
    private readonly List<TransitAlert> _alerts = [];

    public int Id { get; private set; }
    public string DispatchId { get; private set; } = string.Empty;
    public int VehicleId { get; private set; }
    public string VehiclePlate { get; private set; } = string.Empty;
    public string OriginName { get; private set; } = string.Empty;
    public int DestinationGeofenceId { get; private set; }
    public Destination Destination { get; private set; } = null!;
    public RouteStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EstimatedArrival { get; private set; }
    public DateTimeOffset? ArrivedAt { get; private set; }

    /// <summary>Max distance in meters from the planned path.</summary>
    public double DeviationThresholdMeters { get; private set; }

    /// <summary>Max minutes stopped before raising an alert.</summary>
    public int StopThresholdMinutes { get; private set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Planned route waypoints, in travel order.</summary>
    public IReadOnlyList<RouteWaypoint> Waypoints => _waypoints;

    /// <summary>Accumulated telemetry, in chronological order.</summary>
    public IReadOnlyList<TelemetryPosition> Positions => _positions;

    /// <summary>Raised transit alerts, in detection order.</summary>
    public IReadOnlyList<TransitAlert> Alerts => _alerts;

    private TransitRoute() { }

    public TransitRoute(string dispatchId, int vehicleId, string vehiclePlate, string originName,
        int destinationGeofenceId, Destination destination, IReadOnlyList<GeoPoint> plannedPath,
        DateTimeOffset startedAt, DateTimeOffset estimatedArrival,
        double deviationThresholdMeters = 300, int stopThresholdMinutes = 15)
    {
        if (string.IsNullOrWhiteSpace(dispatchId))
            throw InvalidRoute("Dispatch id is required.");
        if (vehicleId <= 0)
            throw InvalidRoute("Vehicle id must be greater than zero.");
        if (string.IsNullOrWhiteSpace(vehiclePlate))
            throw InvalidRoute("Vehicle license plate is required.");
        if (string.IsNullOrWhiteSpace(originName))
            throw InvalidRoute("Origin name is required.");
        if (destinationGeofenceId <= 0)
            throw InvalidRoute("Destination geofence id must be greater than zero.");
        if (plannedPath.Count < 2)
            throw InvalidRoute("The planned path needs at least two waypoints.");
        if (estimatedArrival < startedAt)
            throw InvalidRoute("The estimated arrival cannot be before the departure.");
        if (deviationThresholdMeters <= 0)
            throw InvalidRoute("The deviation threshold must be greater than zero.");
        if (stopThresholdMinutes <= 0)
            throw InvalidRoute("The stop threshold must be greater than zero.");

        DispatchId = dispatchId.Trim();
        VehicleId = vehicleId;
        VehiclePlate = vehiclePlate.Trim();
        OriginName = originName.Trim();
        DestinationGeofenceId = destinationGeofenceId;
        Destination = destination ?? throw InvalidRoute("Destination is required.");
        StartedAt = startedAt;
        EstimatedArrival = estimatedArrival;
        DeviationThresholdMeters = deviationThresholdMeters;
        StopThresholdMinutes = stopThresholdMinutes;
        Status = RouteStatus.InTransit;
        for (var i = 0; i < plannedPath.Count; i++)
            _waypoints.Add(new RouteWaypoint(i, plannedPath[i].Latitude, plannedPath[i].Longitude));
    }

    /// <summary>Whether the route still accepts telemetry.</summary>
    public bool IsActive => Status != RouteStatus.Arrived;

    /// <summary>Last reported position.</summary>
    public TelemetryPosition? CurrentPosition => _positions.Count > 0 ? _positions[^1] : null;

    /// <summary>Deviation / stop alerts not yet notified to the destination site.</summary>
    public IEnumerable<TransitAlert> PendingNotifications => _alerts.Where(a => a.IsPendingNotification);

    /// <summary>Distance travelled along the reported positions, in meters.</summary>
    public double TravelledDistanceMeters
    {
        get
        {
            var total = 0.0;
            for (var i = 1; i < _positions.Count; i++)
                total += _positions[i - 1].Point.DistanceTo(_positions[i].Point);
            return total;
        }
    }

    /// <summary>
    /// Distance from a position to the closest segment of the planned path.
    /// </summary>
    public double DistanceToPlannedPath(TelemetryPosition position)
    {
        if (_waypoints.Count < 2) return 0;
        var min = double.PositiveInfinity;
        for (var i = 1; i < _waypoints.Count; i++)
            min = Math.Min(min, position.Point.DistanceToSegment(_waypoints[i - 1].Point, _waypoints[i].Point));
        return min;
    }

    /// <summary>
    /// Minutes the vehicle has stayed within the stop radius up to the position at the given index.
    /// </summary>
    public double MinutesStoppedAt(int index)
    {
        if (index < 1) return 0;
        var position = _positions[index];
        var first = index;
        while (first > 0 && _positions[first - 1].Point.DistanceTo(position.Point) <= StopRadiusMeters) first--;
        return _positions[first].MinutesUntil(position);
    }

    /// <summary>
    /// Whether the position lies inside the destination site geofence.
    /// </summary>
    public bool IsInsideDestination(TelemetryPosition position) =>
        position.Point.DistanceTo(Destination.Center) <= Destination.RadiusMeters;

    /// <summary>
    /// Incorporates a position reported by the GPS telemetry platform and evaluates
    /// route deviation, prolonged stop and destination geofence entry.
    /// </summary>
    /// <returns>Alerts raised by this position.</returns>
    public IReadOnlyList<TransitAlert> RegisterPosition(double latitude, double longitude, double speedKmh, DateTimeOffset reportedAt)
    {
        EnsureActive();

        var previous = CurrentPosition;
        if (previous is not null && reportedAt < previous.ReportedAt)
            throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.InvalidPosition,
                "The reported position is older than the last registered one.");

        var position = new TelemetryPosition(latitude, longitude, speedKmh, reportedAt);
        var raised = new List<TransitAlert>();

        if (Status == RouteStatus.SignalLost)
            raised.Add(RaiseAlert(TransitAlertType.SignalRecovered, position));

        _positions.Add(position);
        var index = _positions.Count - 1;

        var distanceOffRoute = DistanceToPlannedPath(position);
        var isOffRoute = distanceOffRoute > DeviationThresholdMeters;
        var wasOffRoute = previous is not null && DistanceToPlannedPath(previous) > DeviationThresholdMeters;
        if (isOffRoute && !wasOffRoute)
            raised.Add(RaiseAlert(TransitAlertType.RouteDeviation, position, Math.Round(distanceOffRoute, MidpointRounding.AwayFromZero)));

        var minutesStopped = MinutesStoppedAt(index);
        var isStopped = minutesStopped >= StopThresholdMinutes;
        var wasStopped = MinutesStoppedAt(index - 1) >= StopThresholdMinutes;
        if (isStopped && !wasStopped)
            raised.Add(RaiseAlert(TransitAlertType.ProlongedStop, position, Math.Round(minutesStopped, MidpointRounding.AwayFromZero)));

        if (IsInsideDestination(position))
        {
            raised.Add(RaiseAlert(TransitAlertType.GeofenceEntry, position));
            Status = RouteStatus.Arrived;
            ArrivedAt = position.ReportedAt;
        }
        else
        {
            Status = isOffRoute ? RouteStatus.Deviated : isStopped ? RouteStatus.Stopped : RouteStatus.InTransit;
        }

        return raised;
    }

    /// <summary>
    /// Registers that the GPS device stopped reporting.
    /// </summary>
    /// <returns>Raised signal-lost alert.</returns>
    public TransitAlert ReportSignalLost(DateTimeOffset detectedAt)
    {
        EnsureActive();
        if (Status == RouteStatus.SignalLost)
            throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.SignalAlreadyLost,
                $"Route {Id} signal is already lost.");

        var alert = RaiseAlert(TransitAlertType.SignalLost, CurrentPosition, null, detectedAt);
        Status = RouteStatus.SignalLost;
        return alert;
    }

    /// <summary>
    /// Marks a deviation or prolonged-stop alert as notified to the destination site.
    /// </summary>
    public TransitAlert NotifyDestinationSite(int alertId, DateTimeOffset notifiedAt)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == alertId)
                    ?? throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.AlertNotFound,
                        $"Alert {alertId} not found in route {Id}.");
        alert.MarkAsNotified(notifiedAt);
        return alert;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new TransitTraceabilityException(TransitTraceabilityErrorCodes.RouteAlreadyArrived,
                $"Route {Id} already arrived at its destination.");
    }

    private TransitAlert RaiseAlert(TransitAlertType type, TelemetryPosition? position, double? value = null,
        DateTimeOffset? detectedAt = null)
    {
        var alert = new TransitAlert(type, detectedAt ?? position?.ReportedAt ?? DateTimeOffset.UtcNow,
            position?.Latitude, position?.Longitude, value);
        _alerts.Add(alert);
        return alert;
    }

    private static TransitTraceabilityException InvalidRoute(string message) =>
        new(TransitTraceabilityErrorCodes.InvalidRoute, message);
}
