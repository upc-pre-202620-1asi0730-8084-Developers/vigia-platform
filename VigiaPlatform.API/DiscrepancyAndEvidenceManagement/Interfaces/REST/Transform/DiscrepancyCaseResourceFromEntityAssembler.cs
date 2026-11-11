using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Transform;

public static class DiscrepancyCaseResourceFromEntityAssembler
{
    public static DiscrepancyCaseResource ToResourceFromEntity(
        DiscrepancyCase entity)
    {
        return new DiscrepancyCaseResource(
            entity.Id,
            entity.DispatchId,
            entity.ReceptionId,
            entity.CaseNumber,
            entity.OpenedAt,
            entity.ClosedAt,
            entity.Status,
            entity.DeterminedLiability,
            entity.ResolutionSummary
        );
    }
}