using System.Text.RegularExpressions;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;
using VigiaPlatform.API.TransitTraceability.Domain.Services;
using VigiaPlatform.API.TransitTraceability.Interfaces.REST.Resources;

namespace VigiaPlatform.API.TransitTraceability.Interfaces.REST.Transform;

/// <summary>
/// Enum names travel as SCREAMING_SNAKE_CASE (IN_TRANSIT, ROUTE_DEVIATION), the contract the frontend already uses.
/// </summary>
public static partial class EnumContract
{
    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBoundary();

    public static string ToContract<TEnum>(TEnum value) where TEnum : struct, Enum =>
        WordBoundary().Replace(value.ToString(), "_").ToUpperInvariant();

    public static bool TryParseContract<TEnum>(string? text, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        return !string.IsNullOrWhiteSpace(text) && Enum.TryParse(text.Replace("_", string.Empty), true, out value);
    }
}

public static class TransitAlertResourceFromEntityAssembler
{
    public static TransitAlertResource ToResourceFromEntity(TransitAlert entity) =>
        new(entity.Id, EnumContract.ToContract(entity.Type), entity.DetectedAt, entity.Latitude, entity.Longitude,
            entity.Value, entity.NotifiedAt);
}

public static class RouteResourceFromEntityAssembler
{
    public static RouteResource ToResourceFromEntity(TransitRoute entity) =>
        new(entity.Id,
            entity.DispatchId,
            entity.VehicleId,
            entity.VehiclePlate,
            entity.OriginName,
            entity.DestinationGeofenceId,
            new DestinationResource(entity.Destination.Name, entity.Destination.Latitude,
                entity.Destination.Longitude, entity.Destination.RadiusMeters),
            entity.Waypoints.Select(w => new GeoPointResource(w.Latitude, w.Longitude)).ToList(),
            entity.Positions.Select(p => new TelemetryPositionResource(p.Latitude, p.Longitude, p.SpeedKmh, p.ReportedAt)).ToList(),
            entity.Alerts.Select(TransitAlertResourceFromEntityAssembler.ToResourceFromEntity).ToList(),
            EnumContract.ToContract(entity.Status),
            entity.StartedAt,
            entity.EstimatedArrival,
            entity.ArrivedAt,
            entity.DeviationThresholdMeters,
            entity.StopThresholdMinutes);
}

public static class TransitOutcomeResourceFromEntityAssembler
{
    public static TransitOutcomeResource ToResourceFromOutcome(TransitOutcome outcome) =>
        new(RouteResourceFromEntityAssembler.ToResourceFromEntity(outcome.Route),
            outcome.RaisedAlerts.Select(TransitAlertResourceFromEntityAssembler.ToResourceFromEntity).ToList());
}

public static class CreateRouteCommandFromResourceAssembler
{
    public static CreateRouteCommand ToCommandFromResource(CreateRouteResource resource) =>
        new(resource.DispatchId,
            resource.VehicleId,
            resource.VehiclePlate,
            resource.OriginName,
            resource.DestinationGeofenceId,
            resource.Destination.Name,
            resource.Destination.Latitude,
            resource.Destination.Longitude,
            resource.Destination.RadiusMeters,
            resource.PlannedPath.Select(p => new GeoPoint(p.Latitude, p.Longitude)).ToList(),
            resource.StartedAt,
            resource.EstimatedArrival,
            resource.DeviationThresholdMeters,
            resource.StopThresholdMinutes);
}

public static class RegisterPositionCommandFromResourceAssembler
{
    public static RegisterPositionCommand ToCommandFromResource(int routeId, RegisterPositionResource resource) =>
        new(routeId, resource.Latitude, resource.Longitude, resource.SpeedKmh, resource.ReportedAt);
}
