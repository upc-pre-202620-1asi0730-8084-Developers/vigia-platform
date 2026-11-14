namespace VigiaPlatform.API.IdentityAndAccessManagement.Application;

using Microsoft.AspNetCore.Identity;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;

public class AccountService(IUserAccountRepository repository, IPasswordHasher<UserAccount> hasher, IUnitOfWork unitOfWork)
{
    public async Task<UserAccount?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var account = await repository.FindByUsernameAsync(username, cancellationToken);
        if (account is null) return null;
        var result = hasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.SetPasswordHash(hasher.HashPassword(account, password));
            await unitOfWork.CompleteAsync();
        }
        return account;
    }

    public async Task<UserAccount?> RegisterAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (await repository.FindByUsernameAsync(username, cancellationToken) is not null) return null;
        var account = new UserAccount(username, username);
        account.SetPasswordHash(hasher.HashPassword(account, password));
        await repository.AddAsync(account, cancellationToken);
        await unitOfWork.CompleteAsync();
        return account;
    }
}
