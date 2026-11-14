namespace VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;

/// <summary>
/// Value Object capturing weighbridge metrics at the construction site entrance.
/// Guarantees that gross weight is strictly greater than or equal to tare weight.
/// </summary>
public record ScaleWeight
{
    public double GrossWeightKg { get; init; }
    public double TareWeightKg { get; init; }
    public double NetWeightKg => Math.Round(GrossWeightKg - TareWeightKg, 2);
    public string WeighingSlipNumber { get; init; } = string.Empty;
    public DateTimeOffset WeighedAt { get; init; }

    public ScaleWeight() { }

    public ScaleWeight(double grossWeightKg, double tareWeightKg, string weighingSlipNumber, DateTimeOffset? weighedAt = null)
    {
        if (grossWeightKg <= 0)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidWeight, "Gross weight must be greater than zero.");
        if (tareWeightKg < 0)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidWeight, "Tare weight cannot be negative.");
        if (grossWeightKg < tareWeightKg)
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidWeight, "Gross weight cannot be less than tare weight.");
        if (string.IsNullOrWhiteSpace(weighingSlipNumber))
            throw new SiteReceptionException(SiteReceptionErrorCodes.InvalidWeight, "Weighing slip number is required.");

        GrossWeightKg = Math.Round(grossWeightKg, 2);
        TareWeightKg = Math.Round(tareWeightKg, 2);
        WeighingSlipNumber = weighingSlipNumber.Trim();
        WeighedAt = weighedAt ?? DateTimeOffset.UtcNow;
    }
}
