namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Reporting;

[ApiController]
[Authorize(Roles = "ADMIN,SITE_MANAGER")]
[Route("api/v1/discrepancy-cases/{id:int}/report")]
public class DiscrepancyReportsController(DiscrepancyReportService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var report = await service.GenerateAsync(id, ct);
        return report is null ? NotFound() : File(report, "application/pdf", $"discrepancy-case-{id}-report.pdf");
    }
}
