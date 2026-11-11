using System.ComponentModel.DataAnnotations;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

public record CreateDiscrepancyCaseResource(
    Guid DispatchId,
    Guid ReceptionId,
    [Required]
    [MaxLength(50)]
    string CaseNumber
);