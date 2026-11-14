namespace VigiaPlatform.API.ProjectManagement.Domain.Model;

public class ConstructionProject
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Zone { get; private set; } = "";
    public string City { get; private set; } = "";
    public string? ManagerId { get; private set; }
    public string ManagerName { get; private set; } = "";
    public decimal Progress { get; private set; }
    public string Status { get; private set; } = "IN_PROGRESS";

    private ConstructionProject() { }

    public ConstructionProject(string name, string zone, string city, string? managerId, string managerName,
        decimal progress = 0, string status = "IN_PROGRESS")
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(city) || progress is < 0 or > 100)
            throw new ArgumentException("Project name, city and progress between 0 and 100 are required.");
        if (status is not ("IN_PROGRESS" or "AT_RISK" or "DELAYED"))
            throw new ArgumentException("Unknown project status.");
        Name = name.Trim();
        Zone = zone;
        City = city;
        ManagerId = managerId;
        ManagerName = managerName;
        Progress = progress;
        Status = status;
    }
}

public record ProjectSummary(ConstructionProject Project, int DispatchesCount, int ReceptionsCount, int IncidentsCount);
