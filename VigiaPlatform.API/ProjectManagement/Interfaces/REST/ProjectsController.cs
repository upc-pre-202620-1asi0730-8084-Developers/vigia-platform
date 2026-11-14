namespace VigiaPlatform.API.ProjectManagement.Interfaces.REST;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.ProjectManagement.Application;

public record ProjectResource(int Id, string Name, string Zone, string City, string? ManagerId, string ManagerName,
    decimal Progress, int DispatchesCount, int ReceptionsCount, int IncidentsCount, string Status);

[ApiController]
[Route("api/v1/projects")]
[Authorize(Roles = "ADMIN,SITE_MANAGER,WAREHOUSE_MANAGER")]
public class ProjectsController(ProjectQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await service.GetProjectsAsync(cancellationToken)).Select(s => new ProjectResource(s.Project.Id,
            s.Project.Name, s.Project.Zone, s.Project.City, s.Project.ManagerId, s.Project.ManagerName,
            s.Project.Progress, s.DispatchesCount, s.ReceptionsCount, s.IncidentsCount, s.Project.Status)));
}
