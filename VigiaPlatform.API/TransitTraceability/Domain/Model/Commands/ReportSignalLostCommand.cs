namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;

public record ReportSignalLostCommand(int RouteId, DateTimeOffset? DetectedAt);
