namespace VigiaPlatform.API.ContactRequests.Domain.Repositories;

using VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates;

public interface IContactRequestRepository
{
    Task AddAsync(ContactRequest request, CancellationToken ct = default);
    Task<ContactRequest?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<ContactRequest?> FindByRequestIdAsync(string requestId, CancellationToken ct = default);
    Task<IReadOnlyList<ContactRequest>> ListAsync(CancellationToken ct = default);
}
