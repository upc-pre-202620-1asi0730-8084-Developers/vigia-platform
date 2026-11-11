namespace VigiaPlatform.API.SiteReceptionAndVerification;

using Microsoft.Extensions.DependencyInjection;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.CommandServices;
using VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.QueryServices;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Registers the repositories and application services of the Site Reception and Verification Bounded Context.
/// </summary>
public static class SiteReceptionAndVerificationRegistration
{
    public static IServiceCollection AddSiteReceptionAndVerificationContext(this IServiceCollection services)
    {
        services.AddScoped<IReceptionRepository, ReceptionRepository>();
        services.AddScoped<IReceptionCommandService, ReceptionCommandService>();
        services.AddScoped<IReceptionQueryService, ReceptionQueryService>();
        return services;
    }
}
