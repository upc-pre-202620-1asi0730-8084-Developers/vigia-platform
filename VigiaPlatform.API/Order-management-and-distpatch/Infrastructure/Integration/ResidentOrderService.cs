using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.InventoryManagement.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration;

public class ResidentOrderService(AppDbContext db, IUnitOfWork unitOfWork)
{
    private IQueryable<ConstructionProject> AssignedProjects(int accountId, bool administrator) =>
        db.Set<ConstructionProject>().Where(p => administrator || p.ManagerId == accountId.ToString());

    public async Task<IReadOnlyList<RequestProjectResource>> CatalogAsync(int accountId, bool administrator,
        CancellationToken ct)
    {
        var projects = await AssignedProjects(accountId, administrator).AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
        var projectIds = projects.Select(p => p.Id.ToString()).ToArray();
        var materials = await db.Set<Material>().AsNoTracking()
            .Where(m => m.ProjectId == null || projectIds.Contains(m.ProjectId)).OrderBy(m => m.Name).ToListAsync(ct);
        return projects.Select(p => new RequestProjectResource(p.Id, p.Name,
            materials.Where(m => m.ProjectId == null || m.ProjectId == p.Id.ToString())
                .Where(m => Enum.TryParse<ItemsUnitType>(m.Unit, out var unit) && Enum.IsDefined(unit))
                .Select(m => new RequestMaterialResource(m.Id, m.Name, m.Unit)).ToList())).ToList();
    }

    public async Task<Order> SubmitAsync(int accountId, bool administrator, SubmitMaterialRequestResource input,
        CancellationToken ct)
    {
        var project = await AssignedProjects(accountId, administrator).SingleOrDefaultAsync(p => p.Id == input.ProjectId, ct);
        if (project is null) throw new UnauthorizedAccessException("The project is not assigned to this account.");
        if (input.Items is null || input.Items.Count is < 1 or > 100 ||
            input.Items.Any(i => i is null) ||
            input.Items.Select(i => i.MaterialId).Distinct().Count() != input.Items.Count ||
            input.Items.Any(i => i.Quantity <= 0) || input.Date == default || input.Observations?.Length > 350 ||
            !Enum.TryParse<OrderPriorityType>(input.Priority, true, out var priority) || !Enum.IsDefined(priority))
            throw new ArgumentException("Provide a date, a valid priority and distinct materials with positive integer quantities.");
        var ids = input.Items.Select(i => i.MaterialId).ToArray();
        var materials = await db.Set<Material>().Where(m => ids.Contains(m.Id) &&
            (m.ProjectId == null || m.ProjectId == project.Id.ToString())).ToListAsync(ct);
        if (materials.Count != input.Items.Count) throw new ArgumentException("A material is not available for this project.");
        var items = input.Items.Select(i =>
        {
            var material = materials.Single(m => m.Id == i.MaterialId);
            if (!Enum.TryParse<ItemsUnitType>(material.Unit, out var unit) || !Enum.IsDefined(unit))
                throw new ArgumentException("The material unit is not supported for orders.");
            return new CreateOrderItemCommand(material.Name, i.Quantity, unit, i.Observation);
        }).ToList();
        var account = await db.Set<UserAccount>().SingleAsync(u => u.Id == accountId, ct);
        var now = DateTime.UtcNow;
        var code = $"SOL-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{System.Security.Cryptography.RandomNumberGenerator.GetInt32(10, 100)}";
        var order = new Order(new CreateOrderCommand(code, project.Name, account.FullName, priority,
            input.Date, items[0].Material, input.Observations, items, []), now, accountId);
        db.Add(order);
        await unitOfWork.CompleteAsync(ct);
        return order;
    }
}
