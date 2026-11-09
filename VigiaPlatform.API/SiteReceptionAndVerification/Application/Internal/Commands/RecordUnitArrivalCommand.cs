namespace VigiaPlatform.API.SiteReceptionAndVerification.Application.Internal.Commands;

public record InitialItemRecord(string MaterialId, string MaterialName, string Unit, double DispatchedQuantity);

public record RecordUnitArrivalCommand(
    string DispatchId,
    string CarrierPlate,
    string DriverName,
    int SiteId,
    string SiteName,
    string SiteResidentName,
    IReadOnlyList<InitialItemRecord>? Items = null
);
