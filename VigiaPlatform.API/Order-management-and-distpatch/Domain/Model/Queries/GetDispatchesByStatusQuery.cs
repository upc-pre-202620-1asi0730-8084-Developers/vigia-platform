using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Queries;

public record GetDispatchesByStatusQuery(DispatchStatus Status);
