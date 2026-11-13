using VigiaPlatform.API.Shared.Domain.Model;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;

public partial class Dispatch : IAuditableEntity
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
