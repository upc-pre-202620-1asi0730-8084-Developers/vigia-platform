namespace VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

public record DispatchPlan(string ProjectName, string RelatedOrderId, string DepartureDate, string EstimatedTime,
    string DispatchType, string Priority, string? Observations);

public class DispatchPlanningService(AppDbContext db, IUnitOfWork unitOfWork)
{
    public async Task<Dispatch> PlanAsync(DispatchPlan input, string actor, CancellationToken ct)
    {
        if (!int.TryParse(input.RelatedOrderId, out var orderId)) throw new ArgumentException("An approved order is required.");
        var order = await db.Set<Order>().Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new ArgumentException("Order does not exist.");
        if (order.Status != OrderStatusType.Approved) throw new InvalidOperationException("The order must be approved.");
        var project = await db.Set<ConstructionProject>().SingleOrDefaultAsync(p => p.Name == input.ProjectName, ct)
            ?? throw new ArgumentException("Select an existing project.");
        if (!string.Equals(order.ProjectName.Value, project.Name, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The order belongs to a different project.");
        var resource = new CreateDispatchResource(project.Name, project.Id.ToString(), project.City,
            input.DepartureDate, input.EstimatedTime, order.Id.ToString(), input.DispatchType, input.Priority,
            actor, $"REF-{Guid.NewGuid():N}", input.Observations,
            order.Items.Select(i => new CreateDispatchItemResource($"MAT-{i.Id}", i.Material.Value, i.Quantity.Value, i.Unit.ToString(), i.Observation.Value)).ToList());
        var dispatch = new Dispatch(CreateDispatchCommandFromResourceAssembler.ToCommandFromResource(resource), DateTime.UtcNow);
        dispatch.ChangeStatus(DispatchStatus.InPreparation, actor, null, DateTime.UtcNow);
        db.Add(dispatch);
        await unitOfWork.CompleteAsync(ct);
        return dispatch;
    }

    public async Task<object[]> UpcomingAsync(CancellationToken ct)
    {
        var registered = await db.Set<SiteReceptionAndVerification.Domain.Model.Aggregates.Reception>().Select(r => r.DispatchId).ToListAsync(ct);
        var dispatches = await db.Set<Dispatch>().AsNoTracking().Include(d => d.Items).Where(d => d.Status == DispatchStatus.InTransit).ToListAsync(ct);
        return dispatches.Where(d => !registered.Contains(d.Id.ToString())).Select(d => (object)new
        {
            id = d.Id, dispatchId = d.Id.ToString(), siteId = d.SiteId.Value, siteName = d.ProjectName.Value,
            origin = "Warehouse", truckPlate = d.VehiclePlate?.Value, driverName = "",
            estimatedArrival = $"{d.DepartureDate}T{d.EstimatedTime}:00", status = "IN_TRANSIT",
            materialDescription = d.Items.First().Material.Value, expectedQuantity = d.Items.First().Quantity.Value,
            unit = d.Items.First().Unit.ToString(), items = d.Items.Select(i => new { materialName = i.Material.Value,
                dispatchedQuantity = i.Quantity.Value, receivedQuantity = i.Quantity.Value, unit = i.Unit.ToString() }).ToArray()
        }).ToArray();
    }
}
