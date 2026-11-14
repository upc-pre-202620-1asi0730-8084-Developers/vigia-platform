using VigiaPlatform.API.TransitTraceability.Application.Internal.CommandServices;
using VigiaPlatform.API.TransitTraceability.Application.Internal.QueryServices;
using VigiaPlatform.API.TransitTraceability.Domain.Repositories;
using VigiaPlatform.API.TransitTraceability.Domain.Services;
using VigiaPlatform.API.TransitTraceability.Infrastructure.Persistence.EFC.Repositories;

namespace VigiaPlatform.API.TransitTraceability;

public static class TransitTraceabilityRegistration
{
    /// <summary>
    /// Registers the repositories and services of the Transit Traceability bounded context.
    /// </summary>
    public static IServiceCollection AddTransitTraceabilityContext(this IServiceCollection services)
    {
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IRouteCommandService, RouteCommandService>();
        services.AddScoped<IRouteQueryService, RouteQueryService>();
        return services;
    }
}
