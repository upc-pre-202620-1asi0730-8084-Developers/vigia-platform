namespace VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;

using VigiaPlatform.API.Shared.Domain.Model;

public static class UserRoles
{
    public const string Admin = "ADMIN";
    public const string WarehouseManager = "WAREHOUSE_MANAGER";
    public const string SiteManager = "SITE_MANAGER";
}

public class UserAccount : IAuditableEntity
{
    public int Id { get; private set; }
    public string CompanyId { get; private set; } = "vigia";
    public string Username { get; private set; } = "";
    public string NormalizedUsername { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string Role { get; private set; } = UserRoles.SiteManager;
    public string Timezone { get; private set; } = "America/Lima";
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private UserAccount() { }

    public UserAccount(string username, string fullName, string role = UserRoles.SiteManager)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Trim().Length is < 3 or > 50)
            throw new ArgumentException("Username must contain between 3 and 50 characters.");
        if (role is not (UserRoles.Admin or UserRoles.WarehouseManager or UserRoles.SiteManager))
            throw new ArgumentException("Unknown role.");
        Username = username.Trim();
        NormalizedUsername = Username.ToUpperInvariant();
        FullName = string.IsNullOrWhiteSpace(fullName) ? Username : fullName.Trim();
        Role = role;
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void UpdateProfile(string fullName, string email, string phone, string timezone)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
            throw new ArgumentException("Full name is required and must not exceed 150 characters.");
        if (timezone is not ("America/Lima" or "America/Bogota" or "America/Mexico_City" or "America/Santiago"))
            throw new ArgumentException("Unsupported time zone.");
        FullName = fullName.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
        Timezone = timezone;
    }
}
