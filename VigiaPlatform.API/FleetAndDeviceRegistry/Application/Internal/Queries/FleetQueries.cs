namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Queries;

using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;

public record GetAllVehiclesQuery(bool? IsGpsPaired = null);
public record GetVehicleByIdQuery(int Id);
public record GetVehicleByLicensePlateQuery(string LicensePlate);
public record GetAllGeofencesQuery(GeofenceType? Type = null);
public record GetGeofenceByIdQuery(int Id);
public record GetGeofenceBySiteIdQuery(string SiteId);
