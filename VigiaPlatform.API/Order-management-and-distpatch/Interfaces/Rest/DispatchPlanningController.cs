namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

[ApiController]
[Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
[Route("api/v1/dispatches/from-order")]
public class DispatchPlanningController(DispatchPlanningService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Plan(DispatchPlan input, CancellationToken ct)
    {
        var dispatch = await service.PlanAsync(input, User.Identity!.Name!, ct);
        return Created($"/api/v1/dispatches/{dispatch.Id}", DispatchResourceFromEntityAssembler.ToResourceFromEntity(dispatch));
    }
}

[ApiController]
[Authorize(Roles = "ADMIN,SITE_MANAGER")]
[Route("api/v1/upcoming-arrivals")]
public class UpcomingArrivalsController(DispatchPlanningService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.UpcomingAsync(ct));
}
