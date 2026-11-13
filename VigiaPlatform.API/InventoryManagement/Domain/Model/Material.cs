namespace VigiaPlatform.API.InventoryManagement.Domain.Model;

public class Material
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string Unit { get; private set; } = "";
    public decimal CurrentStock { get; private set; }
    public decimal MinimumStock { get; private set; }
    public string? ProjectId { get; private set; }
    public string ProjectName { get; private set; } = "";

    private Material() { }

    public Material(string name, string category, string unit, decimal currentStock, decimal minimumStock,
        string? projectId, string projectName)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit) || currentStock < 0 || minimumStock < 0)
            throw new ArgumentException("Material name, unit and non-negative stock quantities are required.");
        if (category is not ("CEMENTS" or "STEEL" or "MASONRY" or "AGGREGATES" or "INSTALLATIONS" or "ELECTRICAL" or "FINISHES"))
            throw new ArgumentException("Unknown material category.");
        Name = name.Trim();
        Category = category;
        Unit = unit.Trim();
        CurrentStock = currentStock;
        MinimumStock = minimumStock;
        ProjectId = projectId;
        ProjectName = projectName;
    }
}
