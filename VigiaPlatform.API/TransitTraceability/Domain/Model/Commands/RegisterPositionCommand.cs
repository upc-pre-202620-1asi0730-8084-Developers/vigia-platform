namespace VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;

public record RegisterPositionCommand(int RouteId, double Latitude, double Longitude, double SpeedKmh, DateTimeOffset? ReportedAt);
