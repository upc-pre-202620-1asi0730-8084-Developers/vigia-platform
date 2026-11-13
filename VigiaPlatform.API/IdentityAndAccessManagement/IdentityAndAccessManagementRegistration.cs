namespace VigiaPlatform.API.IdentityAndAccessManagement;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using VigiaPlatform.API.IdentityAndAccessManagement.Application;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Repositories;
using VigiaPlatform.API.IdentityAndAccessManagement.Infrastructure.Persistence;

public static class IdentityAndAccessManagementRegistration
{
    public static IServiceCollection AddIdentityAndAccessManagementContext(this IServiceCollection services)
    {
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddScoped<AccountService>();
        services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
            .AddBearerToken(options => options.BearerTokenExpiration = TimeSpan.FromHours(8));
        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }
}
