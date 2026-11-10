namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;

public record NotifyDestinationSiteCommand(int RouteId, int AlertId);
