namespace VigiaPlatform.API.ProjectManagement;

using VigiaPlatform.API.ProjectManagement.Application;
using VigiaPlatform.API.ProjectManagement.Domain.Repositories;
using VigiaPlatform.API.ProjectManagement.Infrastructure.Persistence;

public static class ProjectManagementRegistration
{
    public static IServiceCollection AddProjectManagementContext(this IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ProjectQueryService>();
        return services;
    }
}
