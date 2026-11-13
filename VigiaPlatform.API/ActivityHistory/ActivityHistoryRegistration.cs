namespace VigiaPlatform.API.ActivityHistory;

using VigiaPlatform.API.ActivityHistory.Application;
using VigiaPlatform.API.ActivityHistory.Domain.Repositories;
using VigiaPlatform.API.ActivityHistory.Infrastructure.Persistence;
using VigiaPlatform.API.Shared.Domain.Repositories;

public static class ActivityHistoryRegistration
{
    public static IServiceCollection AddActivityHistoryContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ActivityQueryService>();
        services.AddScoped<IChangeRecorder, ActivityChangeRecorder>();
        return services;
    }
}
