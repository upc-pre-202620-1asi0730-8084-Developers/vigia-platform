namespace VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;

using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.Commands;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions;
using VigiaPlatform.API.ProjectManagement.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

public class GeofenceCommandService(IGeofenceRepository geofenceRepository, IUnitOfWork unitOfWork, IProjectRepository projects) : IGeofenceCommandService
{
    public async Task<Geofence?> Handle(DefineGeofenceCommand command, CancellationToken cancellationToken = default)
    {
        var centerPoint = new Location(command.Latitude, command.Longitude);
        var siteId = command.SiteId;
        if (command.Type == GeofenceType.JobSite && int.TryParse(siteId, out var canonicalProjectId)) siteId = canonicalProjectId.ToString();
        var geofence = new Geofence(
            command.CompanyId,
            siteId,
            command.Name,
            command.Type,
            centerPoint,
            command.RadiusMeters
        );

        if (geofence.Type == GeofenceType.JobSite &&
            (!int.TryParse(geofence.SiteId, out var projectId) || !(await projects.ListAsync(cancellationToken)).Any(p => p.Project.Id == projectId)))
            throw new FleetException(FleetErrorCodes.InvalidGeofence, "Select an existing project for the job-site geofence.");
        if (await geofenceRepository.FindByCompanyAndSiteAsync(geofence.CompanyId, geofence.SiteId, cancellationToken) is not null)
            throw new FleetException(FleetErrorCodes.SiteAlreadyHasGeofence, "This company/site already has a geofence.");
        await geofenceRepository.AddForSiteAsync(geofence, cancellationToken);
        try { await unitOfWork.CompleteAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { Number: 1062 })
        { throw new FleetException(FleetErrorCodes.SiteAlreadyHasGeofence, "This company/site already has a geofence."); }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { Number: 1205 or 1213 })
        { throw new FleetException(FleetErrorCodes.ConcurrentModification, "Refresh and retry after a concurrent write."); }
        return geofence;
    }
}
