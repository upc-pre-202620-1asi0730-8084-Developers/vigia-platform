namespace VigiaPlatform.API.ProjectManagement.Domain.Repositories;

using VigiaPlatform.API.ProjectManagement.Domain.Model;

public interface IProjectRepository
{
    Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken = default);
}
