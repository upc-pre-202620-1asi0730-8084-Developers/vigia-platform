namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

public record DiscrepancyCaseTimelineEventResource(
    int Id,
    string Stage,
    string Title,
    string Description,
    DateTimeOffset Date
);