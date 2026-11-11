namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

public record DefineGeofenceCommand(
    string CompanyId,
    string SiteId,
    string Name,
    GeofenceType Type,
    double Latitude,
    double Longitude,
    double RadiusMeters = 100.0
);
