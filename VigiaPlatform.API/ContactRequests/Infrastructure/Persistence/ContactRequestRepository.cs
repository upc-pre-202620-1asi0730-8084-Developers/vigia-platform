namespace VigiaPlatform.API.ContactRequests.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates;
using VigiaPlatform.API.ContactRequests.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public sealed class ContactRequestRepository(AppDbContext db) : IContactRequestRepository
{
    public async Task AddAsync(ContactRequest request, CancellationToken ct = default) =>
        await db.Set<ContactRequest>().AddAsync(request, ct);

    public Task<ContactRequest?> FindByIdAsync(long id, CancellationToken ct = default) =>
        db.Set<ContactRequest>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ContactRequest?> FindByRequestIdAsync(string requestId, CancellationToken ct = default) =>
        db.Set<ContactRequest>().AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId, ct);

    public async Task<IReadOnlyList<ContactRequest>> ListAsync(CancellationToken ct = default) =>
        await db.Set<ContactRequest>()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);
}
