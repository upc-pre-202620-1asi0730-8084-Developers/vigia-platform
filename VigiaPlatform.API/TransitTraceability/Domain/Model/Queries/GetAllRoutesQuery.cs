using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Queries;

public record GetAllRoutesQuery(RouteStatus? Status = null);
