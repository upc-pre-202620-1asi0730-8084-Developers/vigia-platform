using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Transform;

public static class DiscrepancyCaseTimelineEventResourceFromEntityAssembler
{
    public static DiscrepancyCaseTimelineEventResource ToResourceFromEntity(
        DiscrepancyCaseEvent entity)
    {
        return new DiscrepancyCaseTimelineEventResource(
            entity.Id,
            entity.Stage,
            entity.Title,
            entity.Description,
            entity.OccurredAt
        );
    }
}