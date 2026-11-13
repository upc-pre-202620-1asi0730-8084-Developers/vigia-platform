namespace VigiaPlatform.API.ProjectManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.ProjectManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

public class ProjectRepository(AppDbContext context) : IProjectRepository
{
    public async Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var projects = await context.Set<ConstructionProject>().AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken);
        var dispatches = await context.Set<Dispatch>().AsNoTracking().ToListAsync(cancellationToken);
        var receptions = await context.Set<Reception>().AsNoTracking().Select(r => new { r.Id, r.SiteId }).ToListAsync(cancellationToken);
        var cases = await context.Set<DiscrepancyCase>().AsNoTracking().Select(c => c.ReceptionId).ToListAsync(cancellationToken);
        return projects.Select(project =>
        {
            var ids = receptions.Where(r => r.SiteId == project.Id).Select(r => r.Id.ToString()).ToHashSet();
            return new ProjectSummary(project,
                dispatches.Count(d => d.SiteId.Value == project.Id.ToString() || d.ProjectName.Value == project.Name),
                ids.Count, cases.Count(ids.Contains));
        }).ToList();
    }
}
