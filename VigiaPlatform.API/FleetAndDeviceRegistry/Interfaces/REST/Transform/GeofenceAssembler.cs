namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Transform;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST.Resources;

public static class GeofenceAssembler
{
    public static GeofenceType ParseGeofenceType(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
            throw new FleetException(FleetErrorCodes.InvalidGeofence, "Geofence type is required (WAREHOUSE or JOB_SITE).");

        var normalized = rawType.Trim().Replace("_", "").Replace("-", "").Replace(" ", "");
        if (Enum.TryParse<GeofenceType>(normalized, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            return parsed;

        throw new FleetException(FleetErrorCodes.InvalidGeofence,
            $"Invalid geofence type '{rawType}'. Allowed values are WAREHOUSE or JOB_SITE.");
    }

    public static string ToApiString(GeofenceType type) => type switch
    {
        GeofenceType.Warehouse => "WAREHOUSE",
        GeofenceType.JobSite => "JOB_SITE",
        _ => type.ToString().ToUpperInvariant()
    };

    public static DefineGeofenceCommand ToCommandFromResource(DefineGeofenceResource resource)
    {
        var type = ParseGeofenceType(resource.Type);

        double latitude;
        double longitude;

        if (resource.CenterPoint is not null)
        {
            latitude = resource.CenterPoint.Latitude;
            longitude = resource.CenterPoint.Longitude;
        }
        else if (resource.Latitude.HasValue && resource.Longitude.HasValue)
        {
            latitude = resource.Latitude.Value;
            longitude = resource.Longitude.Value;
        }
        else
        {
            throw new FleetException(FleetErrorCodes.InvalidCoordinates,
                "Center point coordinates (latitude and longitude) are required.");
        }

        return new DefineGeofenceCommand(
            resource.CompanyId,
            resource.SiteId,
            resource.Name,
            type,
            latitude,
            longitude,
            resource.RadiusMeters
        );
    }

    public static GeofenceResource ToResourceFromEntity(Geofence entity)
    {
        return new GeofenceResource(
            entity.Id,
            entity.CompanyId,
            entity.SiteId,
            entity.Name,
            ToApiString(entity.Type),
            new LocationResource(entity.CenterPoint.Latitude, entity.CenterPoint.Longitude),
            entity.RadiusMeters,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
