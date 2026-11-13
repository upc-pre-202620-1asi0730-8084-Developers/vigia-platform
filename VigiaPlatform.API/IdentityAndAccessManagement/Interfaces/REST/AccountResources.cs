namespace VigiaPlatform.API.IdentityAndAccessManagement.Interfaces.REST;

using System.ComponentModel.DataAnnotations;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;

public record SignInResource([Required, StringLength(50)] string Username, [Required, StringLength(128)] string Password);
public record SignUpResource([Required, StringLength(50, MinimumLength = 3), RegularExpression("^[a-zA-Z0-9._-]+$")] string Username,
    [Required, StringLength(128, MinimumLength = 10)] string Password);
public record UpdateProfileResource([Required, StringLength(150)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email, [StringLength(40)] string? Phone, [Required] string Timezone);
public record UserResource(int Id, string CompanyId, string Username, string FullName, string Email, string Phone,
    string Role, string Timezone, string[] AllowedRoles)
{
    public static UserResource From(UserAccount account) => new(account.Id, account.CompanyId, account.Username,
        account.FullName, account.Email, account.Phone, account.Role, account.Timezone,
        account.Role == UserRoles.Admin ? [UserRoles.Admin, UserRoles.WarehouseManager, UserRoles.SiteManager] : [account.Role]);
}
