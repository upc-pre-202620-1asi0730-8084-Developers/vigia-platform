namespace VigiaPlatform.API.ActivityHistory.Application;

using VigiaPlatform.API.ActivityHistory.Domain.Model;
using VigiaPlatform.API.ActivityHistory.Domain.Repositories;

public class ActivityQueryService(IActivityRepository repository)
{
    public Task<IReadOnlyList<ActivityRecord>> ListAsync(CancellationToken cancellationToken) => repository.ListAsync(cancellationToken);
}
