using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VigiaPlatform.API.ActivityHistory.Domain.Model;
using VigiaPlatform.API.ActivityHistory.Infrastructure.Persistence;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;
using VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

var fleetChecks = 0;
void RejectFleet(Action action)
{
    try { action(); } catch (VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions.FleetException) { fleetChecks++; return; }
    throw new Exception("Invalid fleet value was accepted.");
}
RejectFleet(() => _ = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.VehicleCapacity(0.001));
RejectFleet(() => _ = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.VehicleCapacity(double.PositiveInfinity));
RejectFleet(() => _ = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.LicensePlate("!!!!!"));
RejectFleet(() => _ = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GpsDeviceId(new string('G',65)));
var fleetLocation = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.Location(-12.07,-77.03);
RejectFleet(() => _ = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("company","site","name",
    VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.Warehouse, fleetLocation,double.NaN));
var fleetVehicle = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Vehicle("company",
    new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.LicensePlate("ABC-123"),"Brand","Model",
    new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.VehicleCapacity(0.01),"GPS-1");
fleetVehicle.UpdatePosition(fleetLocation); fleetVehicle.PairGpsDevice("gps-1");
if (fleetVehicle.CurrentPos is null) throw new Exception("Identical GPS replay lost the position."); fleetChecks++;
fleetVehicle.PairGpsDevice("GPS-2");
if (fleetVehicle.CurrentPos is not null) throw new Exception("Replacement GPS inherited the old position."); fleetChecks++;
Console.WriteLine($"PASS: {fleetChecks} fleet domain checks.");

if (args.Length != 1) throw new ArgumentException("Pass the path to the private local appsettings.Development.json file.");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
var connection = new MySqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
connection.Database = "vigia_local_atomicity_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection.ConnectionString).Options;
var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
    [new Claim(ClaimTypes.Name, "Integration Check")], "local-check")) } };
using (var db = new AppDbContext(options))
{
    await db.Database.MigrateAsync();
    if ((await db.Database.GetAppliedMigrationsAsync()).Count() != db.Database.GetMigrations().Count()) throw new Exception("Expected the complete migration chain.");
    if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new Exception("Pending migrations remain.");
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_activity_write BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'intentional integration failure'");
    db.Add(new Reception("atomic-failure", "ABC-123", "Local Driver", 1, "Local Project", "Local Resident"));
    var failed = false;
    try { await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync(); }
    catch (DbUpdateException) { failed = true; }
    finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_activity_write"); }
    if (!failed) throw new Exception("The forced activity write unexpectedly succeeded.");
}
using (var db = new AppDbContext(options))
{
    if (await db.Set<Reception>().AnyAsync() || await db.Set<ActivityRecord>().AnyAsync())
        throw new Exception("A failed audit write left partially committed business data.");
    db.Add(new Reception("atomic-success", "ABC-123", "Local Driver", 1, "Local Project", "Local Resident"));
    await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync();
}
using (var db = new AppDbContext(options))
{
    if (await db.Set<Reception>().CountAsync() != 1 || await db.Set<ActivityRecord>().CountAsync() != 1)
        throw new Exception("The successful business operation and activity were not committed together.");
    var now = DateTimeOffset.UtcNow;
    var route = new TransitRoute("route-check", 1, "ABC-123", "Warehouse", 1, new Destination("Site", -12.13, -76.98, 100),
        [new GeoPoint(-12.08, -77.03), new GeoPoint(-12.13, -76.98)], now, now.AddHours(1));
    db.Add(route);
    await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync();
}
using (var db = new AppDbContext(options))
{
    var route = await db.Set<TransitRoute>().Include(r => r.Positions).Include(r => r.Waypoints).Include(r => r.Alerts).SingleAsync();
    var before = await db.Set<ActivityRecord>().CountAsync();
    route.RegisterPosition(-12.09, -77.02, 30, route.StartedAt.AddSeconds(1));
    await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync();
    if (await db.Set<ActivityRecord>().CountAsync() != before + 1) throw new Exception("Child-only telemetry changes were not audited.");
}
int weighingDispatchId;
long preparationVersion;
using (var db = new AppDbContext(options))
{
    var dispatch = new Dispatch(CreateDispatchCommandFromResourceAssembler.ToCommandFromResource(
        new CreateDispatchResource("Weighing Check", "site-check", "Lima", DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "18:00", "1", "DEPARTURE_TO_WORK", "AVERAGE", "Integration Check", "WEIGH-" + Guid.NewGuid().ToString("N"),
            null, [new CreateDispatchItemResource("MAT-0001", "Cement", 2, "u", null)])), DateTime.UtcNow);
    dispatch.ChangeStatus(DispatchStatus.InPreparation, "Integration Check", null, DateTime.UtcNow);
    dispatch.AssignVehicle(1, "ABC-123", "Integration Check", DateTime.UtcNow);
    db.Add(dispatch);
    await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync();
    weighingDispatchId = dispatch.Id;
    preparationVersion = dispatch.Version;
}
using (var db = new AppDbContext(options))
{
    var dispatch = await db.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings)
        .SingleAsync(d => d.Id == weighingDispatchId);
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_weighing_audit BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'intentional weighing audit failure'");
    dispatch.RecordNetWeight(100m, 100m, 2m, 1, "Integration Check", DateTimeOffset.UtcNow, Guid.NewGuid());
    var failed = false;
    try { await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync(); }
    catch (DbUpdateException) { failed = true; }
    finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_weighing_audit"); }
    if (!failed) throw new Exception("Forced weighing audit failure unexpectedly succeeded.");
}
using (var db = new AppDbContext(options))
{
    var dispatch = await db.Set<Dispatch>().Include(d => d.Timeline).Include(d => d.Weighings)
        .SingleAsync(d => d.Id == weighingDispatchId);
    if (dispatch.Weighings.Count != 0 || dispatch.Timeline.Count != 3 || dispatch.Version != preparationVersion)
        throw new Exception("Failed weighing left a measurement, timeline event or version change committed.");
}
using (var firstDb = new AppDbContext(options))
using (var staleDb = new AppDbContext(options))
{
    var first = await firstDb.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings)
        .SingleAsync(d => d.Id == weighingDispatchId);
    var stale = await staleDb.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings)
        .SingleAsync(d => d.Id == weighingDispatchId);
    first.RecordNetWeight(100m, 100m, 2m, 1, "Integration Check", DateTimeOffset.UtcNow, Guid.NewGuid());
    await new UnitOfWork(firstDb, [new ActivityChangeRecorder(firstDb, accessor)]).CompleteAsync();
    stale.RecordNetWeight(103m, 100m, 2m, 1, "Integration Check", DateTimeOffset.UtcNow, Guid.NewGuid());
    var rejected = false;
    try { await new UnitOfWork(staleDb, [new ActivityChangeRecorder(staleDb, accessor)]).CompleteAsync(); }
    catch (DbUpdateConcurrencyException) { rejected = true; }
    if (!rejected) throw new Exception("A stale weighing update was not rejected.");
}
using (var db = new AppDbContext(options))
{
    var dispatch = await db.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings)
        .SingleAsync(d => d.Id == weighingDispatchId);
    if (dispatch.Weighings.Count != 1 || dispatch.Timeline.Count != 4 || dispatch.LatestWeighing!.WeightKg != 100m ||
        dispatch.RequiresWeightReview || dispatch.Version != preparationVersion + 1)
        throw new Exception("Stale weighing update left partial records or replaced the accepted measurement.");
}
using (var db = new AppDbContext(options))
{
    var dispatch = await db.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings)
        .Include(d => d.DeliveryGuide).SingleAsync(d => d.Id == weighingDispatchId);
    var version = dispatch.Version;
    dispatch.IssueDeliveryGuide("Local Driver", "Local Carrier", 1, "Integration Check", DateTimeOffset.UtcNow, Guid.NewGuid());
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_guide_activity BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'intentional guide audit failure'");
    var rejected = false;
    try { await new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]).CompleteAsync(); }
    catch (DbUpdateException) { rejected = true; }
    finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_guide_activity"); }
    if (!rejected) throw new Exception("A guide with failed audit was not rolled back.");
    using var verify = new AppDbContext(options);
    var persisted = await verify.Set<Dispatch>().Include(d => d.Timeline).Include(d => d.DeliveryGuide).SingleAsync(d => d.Id == weighingDispatchId);
    if (persisted.DeliveryGuide is not null || persisted.Version != version || persisted.Timeline.Count != 4)
        throw new Exception("Failed guide audit left an authorization, timeline event or version change committed.");
}
using (var firstDb = new AppDbContext(options))
using (var staleDb = new AppDbContext(options))
{
    var first = await firstDb.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings).Include(d => d.DeliveryGuide).SingleAsync(d => d.Id == weighingDispatchId);
    var stale = await staleDb.Set<Dispatch>().Include(d => d.Items).Include(d => d.Timeline).Include(d => d.Weighings).Include(d => d.DeliveryGuide).SingleAsync(d => d.Id == weighingDispatchId);
    first.AddItem(DispatchCommandFromResourceAssembler.ToCommandFromResource(first.Id,
        new AddDispatchItemResource("MAT-RACE", "Changed load", 1, "kg", "", "Integration Check")), DateTime.UtcNow);
    await new UnitOfWork(firstDb, [new ActivityChangeRecorder(firstDb, accessor)]).CompleteAsync();
    stale.IssueDeliveryGuide("Local Driver", "Local Carrier", 1, "Integration Check", DateTimeOffset.UtcNow, Guid.NewGuid());
    var rejected = false;
    try { await new UnitOfWork(staleDb, [new ActivityChangeRecorder(staleDb, accessor)]).CompleteAsync(); }
    catch (DbUpdateConcurrencyException) { rejected = true; }
    if (!rejected) throw new Exception("Guide issuance on a stale load was not rejected.");
}
using (var db = new AppDbContext(options))
{
    var dispatch = await db.Set<Dispatch>().Include(d => d.Items).Include(d => d.Weighings).Include(d => d.DeliveryGuide).SingleAsync(d => d.Id == weighingDispatchId);
    if (dispatch.DeliveryGuide is not null || !dispatch.RequiresWeightReview)
        throw new Exception("Stale guide left a persisted authorization or changed the current-load review rule.");
}
var legacyConnection = new MySqlConnectionStringBuilder(connection.ConnectionString) { Database = connection.Database + "_legacy" };
using (var legacy = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseMySQL(legacyConnection.ConnectionString).Options))
{
    await legacy.Database.EnsureCreatedAsync();
    legacy.Add(new VigiaPlatform.API.ProjectManagement.Domain.Model.ConstructionProject("Legacy Safety Check", "LIMA", "Lima", null, "Legacy Resident"));
    await legacy.SaveChangesAsync();
    await using var inspection = new MySqlConnection(legacyConnection.ConnectionString);
    await inspection.OpenAsync();
    await using var countTables = inspection.CreateCommand();
    countTables.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()";
    var schemaTablesBefore = Convert.ToInt64(await countTables.ExecuteScalarAsync());
    var rejected = false;
    try { await DatabaseMigrationRunner.MigrateAsync(legacy); }
    catch (InvalidOperationException error) when (error.Message.StartsWith("Existing tables do not belong")) { rejected = true; }
    if (!rejected || Convert.ToInt64(await countTables.ExecuteScalarAsync()) != schemaTablesBefore ||
        (await legacy.Database.GetAppliedMigrationsAsync()).Any() ||
        await legacy.Set<VigiaPlatform.API.ProjectManagement.Domain.Model.ConstructionProject>().CountAsync() != 1)
        throw new Exception("The legacy-schema guard failed to preserve the existing schema and data.");
}
using (var db = new AppDbContext(options))
{
    var fence = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("atomic-company","atomic-site","First",
        VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.Warehouse,fleetLocation,100);
    await new VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Repositories.GeofenceRepository(db).AddForSiteAsync(fence);
    await new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]).CompleteAsync();
}
using (var db = new AppDbContext(options))
{
    var duplicate = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("atomic-company","atomic-site","Duplicate",
        VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.Warehouse,fleetLocation,100);
    await new VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Repositories.GeofenceRepository(db).AddForSiteAsync(duplicate);
    var rejected = false;
    try { await new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]).CompleteAsync(); }
    catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException {Number:1062}) { rejected = true; }
    if (!rejected) throw new Exception("Duplicate geofence registration was accepted.");
}
using (var db = new AppDbContext(options))
{
    if (await db.Geofences.CountAsync(g => g.CompanyId=="atomic-company" && g.SiteId=="atomic-site") != 1 ||
        await db.Set<VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Entities.GeofenceSiteRegistration>().CountAsync(g => g.CompanyId=="atomic-company") != 1)
        throw new Exception("Duplicate registration left an orphan geofence.");
}
Console.WriteLine("PASS: duplicate geofence registration rolls back without an orphan business record.");
Console.WriteLine($"PASS: complete migration chain, real MySQL rollback, atomic success, weighing and guide audit rollback, stale load/guide rejection, telemetry history and legacy-schema safety. Database: {connection.Database}");
