namespace VigiaPlatform.API.Shared.Infrastructure.Development;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.InventoryManagement.Domain.Model;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

public static class LocalIntegrationSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Development:SeedDemoData")) return;
        var password = configuration["Development:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new InvalidOperationException("Development:DemoPassword must contain at least 10 characters.");
        var db = services.GetRequiredService<AppDbContext>();
        if (!db.Database.GetDbConnection().Database.StartsWith("vigia_local_", StringComparison.Ordinal))
            throw new InvalidOperationException("Local integration seeding requires a database prefixed vigia_local_.");
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var hasher = services.GetRequiredService<IPasswordHasher<UserAccount>>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var (username, name, role) in new[] {
            ("admin.local", "Local Administrator", UserRoles.Admin),
            ("warehouse.local", "Local Warehouse Manager", UserRoles.WarehouseManager),
            ("site.local", "Local Site Resident", UserRoles.SiteManager) })
        {
            if (await db.Set<UserAccount>().AnyAsync(u => u.NormalizedUsername == username.ToUpperInvariant())) continue;
            var account = new UserAccount(username, name, role);
            account.SetPasswordHash(hasher.HashPassword(account, password));
            db.Add(account);
        }
        await unitOfWork.CompleteAsync();
        if (!await db.Set<ConstructionProject>().AnyAsync())
        {
            var resident = await db.Set<UserAccount>().SingleAsync(u => u.Username == "site.local");
            db.Add(new ConstructionProject("Torre A", "LIMA", "Lima", resident.Id.ToString(), resident.FullName, 35));
            await unitOfWork.CompleteAsync();
        }
        var project = await db.Set<ConstructionProject>().OrderBy(p => p.Id).FirstAsync();
        if (!await db.Vehicles.AnyAsync())
        {
            db.Add(new Vehicle("vigia", new LicensePlate("ABC-123"), "Volvo", "FMX", new VehicleCapacity(30), "GPS-LOCAL-01"));
            await unitOfWork.CompleteAsync();
        }
        if (!await db.Geofences.AnyAsync())
        {
            db.Add(new Geofence("vigia", "warehouse-local", "Local Warehouse", GeofenceType.Warehouse, new Location(-12.08, -77.03), 150));
            db.Add(new Geofence("vigia", project.Id.ToString(), project.Name, GeofenceType.JobSite, new Location(-12.13, -76.98), 150));
            await unitOfWork.CompleteAsync();
        }
        if (!await db.Set<Material>().AnyAsync())
        {
            var material = new Material("Portland Cement", "CEMENTS", "u", 500, 100, project.Id.ToString(), project.Name);
            db.Add(material);
            await unitOfWork.CompleteAsync();
            db.Add(new InventoryMovement(material, "IN", 500, DateTime.UtcNow));
            await unitOfWork.CompleteAsync();
        }
        if (!await db.Set<Order>().AnyAsync())
        {
            for (var i = 1; i <= 3; i++)
            {
                var order = new Order(new CreateOrderCommand($"SOL-{100 + i}", project.Name, "Local Site Resident",
                    OrderPriorityType.Average, DateOnly.FromDateTime(DateTime.UtcNow), "Portland Cement", "Local demonstration data",
                    [new CreateOrderItemCommand("Portland Cement", 20 + i, ItemsUnitType.u, null)], []), DateTime.UtcNow);
                if (i <= 2)
                {
                    order.ChangeStatus(OrderStatusType.UnderReview, "Local Warehouse Manager", null, DateTime.UtcNow);
                    order.ChangeStatus(OrderStatusType.Approved, "Local Warehouse Manager", null, DateTime.UtcNow);
                }
                db.Add(order);
            }
            await unitOfWork.CompleteAsync();
        }
        if (!await db.Set<Dispatch>().AnyAsync())
        {
            var approved = await db.Set<Order>().FirstAsync(o => o.Status == OrderStatusType.Approved);
            var planner = services.GetRequiredService<DispatchPlanningService>();
            var dispatch = await planner.PlanAsync(new DispatchPlan(project.Name, approved.Id.ToString(),
                DateTime.UtcNow.ToString("yyyy-MM-dd"), "18:00", "DEPARTURE_TO_WORK", "AVERAGE", "Local demonstration data"), "Local Warehouse Manager", default);
            var vehicle = await db.Vehicles.FirstAsync();
            dispatch.AssignVehicle(vehicle.Id, vehicle.LicensePlate.Value, "Local Warehouse Manager", DateTime.UtcNow);
            dispatch.ChangeStatus(DispatchStatus.InTransit, "Local Warehouse Manager", null, DateTime.UtcNow);
            await unitOfWork.CompleteAsync();
        }
        if (!await db.Set<TransitRoute>().AnyAsync())
        {
            var dispatch = await db.Set<Dispatch>().FirstAsync(d => d.Status == DispatchStatus.InTransit);
            var destination = await db.Geofences.FirstAsync(g => g.Type == GeofenceType.JobSite);
            var now = DateTimeOffset.UtcNow;
            db.Add(new TransitRoute(dispatch.Id.ToString(), dispatch.VehicleId!.Value, dispatch.VehiclePlate!.Value,
                "Local Warehouse", destination.Id, new Destination(destination.Name, destination.CenterPoint.Latitude, destination.CenterPoint.Longitude, destination.RadiusMeters),
                [new GeoPoint(-12.08, -77.03), new GeoPoint(-12.13, -76.98)], now.AddMinutes(-5), now.AddHours(1)));
            await unitOfWork.CompleteAsync();
        }
        await transaction.CommitAsync();
    }
}
