using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Items;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

/// <summary>
///     Material line that belongs to a dispatch.
/// </summary>
public class DispatchItem
{
    public int Id { get; private set; }
    public int DispatchId { get; private set; }
    public Code Code { get; private set; } = null!;
    public Material Material { get; private set; } = null!;
    public MaterialQuantity Quantity { get; private set; } = null!;
    public UnitType Unit { get; private set; }
    public ItemObservation Observation { get; private set; } = null!;

    // Required by EF Core
    protected DispatchItem()
    {
    }

    public DispatchItem(string code, string material, int quantity, UnitType unit, string? observation)
    {
        Code = new Code(code);
        Material = new Material(material);
        Quantity = new MaterialQuantity(quantity);
        Unit = unit;
        Observation = new ItemObservation(observation);
    }
}
