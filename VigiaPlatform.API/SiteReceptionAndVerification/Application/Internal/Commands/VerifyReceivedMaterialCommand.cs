namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;

public record VerifyReceivedMaterialCommand(
    int ReceptionId,
    string MaterialId,
    double ReceivedQuantity,
    string? Comments = null
);
