namespace VigiaPlatform.API.ActivityHistory.Domain.Repositories;

using VigiaPlatform.API.ActivityHistory.Domain.Model;

public interface IActivityRepository
{
    Task<IReadOnlyList<ActivityRecord>> ListAsync(CancellationToken cancellationToken);
}
