namespace VigiaPlatform.API.IdentityAndAccessManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class UserAccountRepository(AppDbContext context) : IUserAccountRepository
{
    public Task<UserAccount?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        context.Set<UserAccount>().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        return context.Set<UserAccount>().SingleOrDefaultAsync(u => u.NormalizedUsername == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Set<UserAccount>().AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken = default) =>
        await context.Set<UserAccount>().AddAsync(account, cancellationToken);
}
