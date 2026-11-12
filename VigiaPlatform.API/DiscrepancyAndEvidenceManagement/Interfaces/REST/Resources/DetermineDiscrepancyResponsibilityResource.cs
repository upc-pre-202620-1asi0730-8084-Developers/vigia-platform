using System.ComponentModel.DataAnnotations;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

public record DetermineDiscrepancyResponsibilityResource(
    [Required]
    string Responsibility
);