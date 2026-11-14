namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Entity representing an individual material line item verified at the construction site.
/// Encapsulates the comparison between dispatched quantity and physical received quantity.
/// </summary>
public class ReceptionItem
{
    public int Id { get; private set; }
    public int ReceptionId { get; private set; }
    public string MaterialId { get; private set; } = string.Empty;
    public string MaterialName { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public double DispatchedQuantity { get; private set; }
    public double ReceivedQuantity { get; private set; }
    public bool IsVerified { get; private set; }
    public string? Comments { get; private set; }

    public double DiscrepancyQuantity => Math.Round(ReceivedQuantity - DispatchedQuantity, 2);
    public bool HasDiscrepancy => IsVerified && Math.Abs(DiscrepancyQuantity) > 0.001;

    private ReceptionItem() { }

    public ReceptionItem(string materialId, string materialName, string unit, double dispatchedQuantity)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            throw new SiteReceptionException(SiteReceptionErrorCodes.ItemNotFound, "Material ID is required.");
        if (string.IsNullOrWhiteSpace(materialName))
            throw new SiteReceptionException(SiteReceptionErrorCodes.ItemNotFound, "Material name is required.");
        if (string.IsNullOrWhiteSpace(unit))
            throw new SiteReceptionException(SiteReceptionErrorCodes.ItemNotFound, "Unit of measure is required.");
        if (!double.IsFinite(dispatchedQuantity) || dispatchedQuantity < 0)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Dispatched quantity cannot be negative.");

        MaterialId = materialId.Trim();
        MaterialName = materialName.Trim();
        Unit = unit.Trim();
        DispatchedQuantity = Math.Round(dispatchedQuantity, 2);
        ReceivedQuantity = 0;
        IsVerified = false;
    }

    /// <summary>
    /// Executes the "Verify received material" command for this item line.
    /// </summary>
    public void Verify(double receivedQuantity, string? comments = null)
    {
        if (!double.IsFinite(receivedQuantity) || receivedQuantity < 0)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidArrival, "Received quantity cannot be negative.");

        ReceivedQuantity = Math.Round(receivedQuantity, 2);
        Comments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
        IsVerified = true;
    }
}
