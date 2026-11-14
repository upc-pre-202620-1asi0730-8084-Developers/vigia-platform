namespace VigiaPlatform.API.ActivityHistory.Interfaces.REST;

using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.ActivityHistory.Application;

public record ActivityResource(int Id, DateTimeOffset OccurredAt, string Type, string Action, string Detail,
    string UserName, string Reference, string Status);

[ApiController]
[Route("api/v1/activity-records")]
public class ActivityRecordsController(ActivityQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await service.ListAsync(cancellationToken)).Select(a => new ActivityResource(a.Id, a.OccurredAt,
            a.Type, a.Action, a.Detail, a.UserName, a.Reference, a.Status)));
}
