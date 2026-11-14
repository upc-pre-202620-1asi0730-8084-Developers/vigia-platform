namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;

public record ReportDiscrepancyCommand(
    int ReceptionId,
    string Reason,
    string Description,
    string Severity = "Major"
);
