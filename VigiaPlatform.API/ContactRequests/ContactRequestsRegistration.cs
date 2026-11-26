namespace VigiaPlatform.API.ContactRequests;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using VigiaPlatform.API.ContactRequests.Application;
using VigiaPlatform.API.ContactRequests.Domain.Repositories;
using VigiaPlatform.API.ContactRequests.Infrastructure.Persistence;

public static class ContactRequestsRegistration
{
    public static IServiceCollection AddContactRequestsContext(this IServiceCollection services)
    {
        services.AddScoped<IContactRequestRepository, ContactRequestRepository>();
        services.AddScoped<ContactRequestService>();
        services.Configure<RateLimiterOptions>(options =>
        {
            options.AddPolicy("contact-requests", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
        return services;
    }
}
