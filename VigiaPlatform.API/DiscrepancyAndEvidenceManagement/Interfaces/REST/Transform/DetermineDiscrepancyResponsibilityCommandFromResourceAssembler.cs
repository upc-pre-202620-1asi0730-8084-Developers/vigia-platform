using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Transform;

public static class DetermineDiscrepancyResponsibilityCommandFromResourceAssembler
{
    public static DetermineDiscrepancyResponsibilityCommand ToCommandFromResource(
        int id,
        DetermineDiscrepancyResponsibilityResource resource)
    {
        return new DetermineDiscrepancyResponsibilityCommand(
            id,
            resource.Responsibility
        );
    }
}