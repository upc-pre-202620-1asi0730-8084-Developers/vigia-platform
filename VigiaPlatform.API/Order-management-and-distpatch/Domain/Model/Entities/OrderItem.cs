using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

/// <summary>Material line requested in an order.</summary>
public class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public ItemsMaterial Material { get; private set; } = null!;
    public ItemsQuantity Quantity { get; private set; } = null!;
    public ItemsUnitType Unit { get; private set; }
    public ItemsObservation Observation { get; private set; } = null!;

    // Required by EF Core
    protected OrderItem()
    {
    }

    public OrderItem(string material, int quantity, ItemsUnitType unit, string? observation)
    {
        Material = new ItemsMaterial(material);
        Quantity = new ItemsQuantity(quantity);
        Unit = unit;
        Observation = new ItemsObservation(observation);
    }
}
