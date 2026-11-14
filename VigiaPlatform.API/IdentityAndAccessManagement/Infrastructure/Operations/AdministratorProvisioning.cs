namespace VigiaPlatform.API.IdentityAndAccessManagement.Infrastructure.Operations;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public static partial class AdministratorProvisioning
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdministratorProvisioning");
        if (await db.Set<UserAccount>().AnyAsync(u => u.Role == UserRoles.Admin))
        {
            logger.LogInformation("An administrator already exists. No accounts were changed.");
            return;
        }
        var username = configuration["BootstrapAdmin:Username"] ?? "";
        var password = configuration["BootstrapAdmin:Password"] ?? "";
        var fullName = configuration["BootstrapAdmin:FullName"] ?? "Vigia Administrator";
        if (username.Length is < 3 or > 50 || !UsernamePattern().IsMatch(username))
            throw new ArgumentException("BootstrapAdmin:Username must contain 3 to 50 letters, digits, dots, underscores or hyphens.");
        if (password.Length is < 16 or > 128)
            throw new ArgumentException("BootstrapAdmin:Password must contain 16 to 128 characters.");
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
            throw new ArgumentException("BootstrapAdmin:FullName is required and must not exceed 150 characters.");
        if (await db.Set<UserAccount>().AnyAsync(u => u.NormalizedUsername == username.ToUpperInvariant()))
            throw new InvalidOperationException("The requested username already belongs to another account. No permissions were changed.");
        var account = new UserAccount(username, fullName, UserRoles.Admin);
        account.SetPasswordHash(services.GetRequiredService<IPasswordHasher<UserAccount>>().HashPassword(account, password));
        db.Add(account);
        await services.GetRequiredService<IUnitOfWork>().CompleteAsync();
        logger.LogInformation("Initial administrator created. No demo records were seeded.");
    }

    [GeneratedRegex("^[a-zA-Z0-9._-]+$")]
    private static partial Regex UsernamePattern();
}
