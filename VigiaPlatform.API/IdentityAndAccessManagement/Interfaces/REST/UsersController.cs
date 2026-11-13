namespace VigiaPlatform.API.IdentityAndAccessManagement.Interfaces.REST;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

[ApiController]
[Route("api/v1/users")]
public class UsersController(IUserAccountRepository repository, IUnitOfWork unitOfWork) : ControllerBase
{
    private int CurrentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await repository.ListAsync(cancellationToken)).Select(UserResource.From));

    [HttpGet("me")]
    public Task<IActionResult> Me(CancellationToken cancellationToken) => Find(CurrentId, cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Find(int id, CancellationToken cancellationToken)
    {
        if (id != CurrentId && !User.IsInRole(UserRoles.Admin)) return Forbid();
        var account = await repository.FindAsync(id, cancellationToken);
        return account is null ? NotFound() : Ok(UserResource.From(account));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProfileResource resource, CancellationToken cancellationToken)
    {
        if (id != CurrentId) return Forbid();
        var account = await repository.FindAsync(id, cancellationToken);
        if (account is null) return NotFound();
        account.UpdateProfile(resource.FullName, resource.Email, resource.Phone ?? "", resource.Timezone);
        await unitOfWork.CompleteAsync();
        return Ok(UserResource.From(account));
    }
}
