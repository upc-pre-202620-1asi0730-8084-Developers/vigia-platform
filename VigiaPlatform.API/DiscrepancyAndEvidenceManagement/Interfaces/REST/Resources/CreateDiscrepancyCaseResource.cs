using System.ComponentModel.DataAnnotations;

namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST.Resources;

public record CreateDiscrepancyCaseResource(
    [Required]
    [MaxLength(40)]
    string DispatchId,

    [Required]
    [MaxLength(40)]
    string ReceptionId,

    [Required]
    [MaxLength(50)]
    string CaseNumber
);