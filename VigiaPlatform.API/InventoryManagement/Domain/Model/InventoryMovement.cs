namespace VigiaPlatform.API.InventoryManagement.Domain.Model;

public class InventoryMovement
{
    public int Id { get; private set; }
    public int MaterialId { get; private set; }
    public string MaterialName { get; private set; } = "";
    public string? ProjectId { get; private set; }
    public string ProjectName { get; private set; } = "";
    public string Type { get; private set; } = "OUT";
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = "";
    public DateTime Date { get; private set; }

    private InventoryMovement() { }

    public InventoryMovement(Material material, string type, decimal quantity, DateTime date)
    {
        if (type is not ("IN" or "OUT") || quantity <= 0)
            throw new ArgumentException("Movement direction and a positive quantity are required.");
        MaterialId = material.Id;
        MaterialName = material.Name;
        ProjectId = material.ProjectId;
        ProjectName = material.ProjectName;
        Type = type;
        Quantity = quantity;
        Unit = material.Unit;
        Date = date.Date;
    }
}
