namespace VigiaPlatform.API.IdentityAndAccessManagement.Domain.Repositories;

using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;

public interface IUserAccountRepository
{
    Task<UserAccount?> FindAsync(int id, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserAccount account, CancellationToken cancellationToken = default);
}
