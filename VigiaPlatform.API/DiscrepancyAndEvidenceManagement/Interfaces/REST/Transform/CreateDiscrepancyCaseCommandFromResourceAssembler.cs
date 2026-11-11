using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Commands;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Transform;

public static class CreateDiscrepancyCaseCommandFromResourceAssembler
{
    public static CreateDiscrepancyCaseCommand ToCommandFromResource(
        CreateDiscrepancyCaseResource resource)
    {
        return new CreateDiscrepancyCaseCommand(
            resource.DispatchId,
            resource.ReceptionId,
            resource.CaseNumber
        );
    }
}