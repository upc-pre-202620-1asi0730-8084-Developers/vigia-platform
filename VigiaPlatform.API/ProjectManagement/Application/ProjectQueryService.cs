namespace VigiaPlatform.API.ProjectManagement.Application;

using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.ProjectManagement.Domain.Repositories;

public class ProjectQueryService(IProjectRepository repository)
{
    public Task<IReadOnlyList<ProjectSummary>> GetProjectsAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);
}
