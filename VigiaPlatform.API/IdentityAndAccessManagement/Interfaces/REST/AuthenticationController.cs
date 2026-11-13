namespace VigiaPlatform.API.IdentityAndAccessManagement.Interfaces.REST;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VigiaPlatform.API.IdentityAndAccessManagement.Application;

[ApiController]
[Route("api/v1/authentication")]
[AllowAnonymous]
[EnableRateLimiting("authentication")]
public class AuthenticationController(AccountService service) : ControllerBase
{
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInResource resource, CancellationToken cancellationToken)
    {
        var account = await service.AuthenticateAsync(resource.Username, resource.Password, cancellationToken);
        if (account is null) return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Invalid username or password." });
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.FullName),
            new Claim(ClaimTypes.Role, account.Role),
            new Claim("company_id", account.CompanyId)
        };
        return SignIn(new ClaimsPrincipal(new ClaimsIdentity(claims, BearerTokenDefaults.AuthenticationScheme)),
            BearerTokenDefaults.AuthenticationScheme);
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp(SignUpResource resource, CancellationToken cancellationToken)
    {
        var account = await service.RegisterAsync(resource.Username, resource.Password, cancellationToken);
        return account is null
            ? Conflict(new { code = "USERNAME_EXISTS", message = "Username is already registered." })
            : Ok(new { message = "Account created. You can now sign in." });
    }
}
