namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Queries;

public record GetReceptionByIdQuery(int Id);
public record GetReceptionByDispatchIdQuery(string DispatchId);
public record GetAllReceptionsQuery();
public record GetUpcomingArrivalsQuery();
public record GetReceptionsBySiteIdQuery(int SiteId);
