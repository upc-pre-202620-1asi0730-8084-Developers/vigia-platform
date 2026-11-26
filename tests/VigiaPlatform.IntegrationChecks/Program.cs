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

var a01DomainChecks = 0;
var a01DomainReception = new Reception("10", "A01-123", "Driver One", 1, "Site A", "Resident One");
a01DomainReception.BeginPhysicalVerification("GD-00000010", "101", "Resident One");
a01DomainReception.RecordRegisterRequestId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
a01DomainReception.AddItem("1", "Cement", "u", 5);
a01DomainReception.VerifyMaterial("1", 5);
var versionBeforeVerify = a01DomainReception.Version;
a01DomainReception.RecordVerifyRequestId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
a01DomainReception.CompletePhysicalVerification(true, true, true, false, null, false);
if (a01DomainReception.Status != VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects.ReceptionStatus.ConformantClosed ||
    a01DomainReception.Version <= versionBeforeVerify ||
    a01DomainReception.VerifyRequestId != "22222222-2222-2222-2222-222222222222")
    throw new Exception("A01 domain conformant closure or concurrency version failed.");
a01DomainChecks++;
var emptyGuidRejected = false;
try { a01DomainReception.RecordVerifyRequestId(Guid.Empty); } catch (ArgumentException) { emptyGuidRejected = true; }
if (!emptyGuidRejected) throw new Exception("Empty verify request ID was accepted.");
a01DomainChecks++;
Console.WriteLine($"PASS: {a01DomainChecks} A01 reception domain checks.");

var a04MemRepo = new InMemoryContactRequestRepository();
var a04Svc = new VigiaPlatform.API.ContactRequests.Application.ContactRequestService(a04MemRepo, new NoOpUnitOfWork());
var a04Invalid = await a04Svc.CreateAsync(new(null, "", "", "bad-email", null, "", "BAD", "short", "es"));
if (a04Invalid.ValidationErrors is null
    || !a04Invalid.ValidationErrors.ContainsKey("fullName")
    || !a04Invalid.ValidationErrors.ContainsKey("companyName")
    || !a04Invalid.ValidationErrors.ContainsKey("email")
    || !a04Invalid.ValidationErrors.ContainsKey("segment")
    || !a04Invalid.ValidationErrors.ContainsKey("plan")
    || !a04Invalid.ValidationErrors.ContainsKey("message"))
    throw new Exception("A04 domain validation did not flag all invalid fields.");
var a04ReqId = Guid.NewGuid().ToString("D");
var a04Cmd = new VigiaPlatform.API.ContactRequests.Application.CreateContactRequestCommand(
    a04ReqId, "Carlos Mendoza", "Constructora Andina SAC", "cmendoza@andina.pe",
    "+51 987 654 321", "CONSTRUCTION", "PRO",
    "Necesitamos trazabilidad de despachos y recepciones en obra.", "es");
var a04Created = await a04Svc.CreateAsync(a04Cmd);
if (a04Created.Request is null || a04Created.Request.Status != "RECEIVED")
    throw new Exception("A04 valid contact request failed.");
var a04Replay = await a04Svc.CreateAsync(a04Cmd);
if (a04Replay.Request is null || a04Replay.Request.Id != a04Created.Request.Id)
    throw new Exception("A04 idempotent replay failed.");
var a04Conflict = await a04Svc.CreateAsync(a04Cmd with { CompanyName = "Otra SAC" });
if (a04Conflict.ConflictCode != "REQUEST_ID_CONFLICT")
    throw new Exception("A04 requestId conflict was not detected.");
Console.WriteLine("PASS: 4 A04 contact-request service domain/validation/idempotency checks.");

if (args.Length == 1 && args[0] == "--domain-only") return;
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
using (var db = new AppDbContext(options))
{
    db.Add(new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Vehicle("company",
        new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.LicensePlate("TRP-123"),"Local","Atomic trip",
        new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.VehicleCapacity(8),"GPS-ATOMIC-TRIP"));
    db.Add(new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("company","atomic-origin","Origin",
        VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.Warehouse,fleetLocation,100));
    db.Add(new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("company","site-check","Destination",
        VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.JobSite,
        new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.Location(-12.13,-76.98),100));
    var dispatch=await db.Set<Dispatch>().Include(d=>d.Items).Include(d=>d.Timeline).Include(d=>d.Weighings).Include(d=>d.DeliveryGuide).SingleAsync(d=>d.Id==weighingDispatchId);
    dispatch.RecordNetWeight(100m,100m,2m,1,"Integration Check",DateTimeOffset.UtcNow,Guid.NewGuid());
    await new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]).CompleteAsync();
    dispatch.IssueDeliveryGuide("Driver","Carrier",1,"Integration Check",DateTimeOffset.UtcNow,Guid.NewGuid());
    await new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]).CompleteAsync();
}
VigiaPlatform.API.TransitTraceability.Infrastructure.Integration.DispatchTransitService TripService(AppDbContext db) => new(db,
    new VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Repositories.DispatchRepository(db),
    new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]));
VigiaPlatform.API.TransitTraceability.Infrastructure.Integration.DeparturePlan tripPlan;
long tripVehicleVersion;
using (var db=new AppDbContext(options))
{
    tripVehicleVersion=(await db.Vehicles.SingleAsync()).PositionVersion;
    var origin=await db.Geofences.SingleAsync(g=>g.CompanyId=="company"&&g.SiteId=="atomic-origin");
    tripPlan=new(origin.Id,DateTimeOffset.UtcNow.AddHours(1),
        [new(-12.07,-77.03),new(-12.13,-76.98)],Guid.NewGuid());
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_trip_audit BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='intentional trip audit failure'");
    var failed=false;
    try {await TripService(db).DepartAsync(weighingDispatchId,tripPlan,"Integration Check","company",default);}
    catch(DbUpdateException){failed=true;}
    finally{await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_trip_audit");}
    if(!failed)throw new Exception("Departure audit failure did not roll back.");
}
int controlledRouteId;
using(var db=new AppDbContext(options))
{
    if(await db.Set<TransitRoute>().CountAsync()!=1 || await db.Set<VigiaPlatform.API.TransitTraceability.Domain.Model.Entities.DispatchTrip>().AnyAsync() ||
        await db.Set<Dispatch>().Where(d=>d.Id==weighingDispatchId).Select(d=>d.Status).SingleAsync()!=DispatchStatus.InPreparation ||
        (await db.Vehicles.SingleAsync()).PositionVersion!=tripVehicleVersion)throw new Exception("Failed departure left partial reservation, route or dispatch state.");
    controlledRouteId=(await TripService(db).DepartAsync(weighingDispatchId,tripPlan,"Integration Check","company",default)).Id;
}
var gpsTime=DateTimeOffset.UtcNow;
using(var db=new AppDbContext(options))
{
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_gps_audit BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='intentional GPS audit failure'");
    var failed=false;
    try {await TripService(db).ReportAsync("GPS-ATOMIC-TRIP",-12.08,-77.03,gpsTime,20,"company",default);}
    catch(DbUpdateException){failed=true;}
    finally {await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_gps_audit");}
    if(!failed)throw new Exception("GPS audit failure did not roll back.");
}
using(var db=new AppDbContext(options))
{
    if(await db.Set<VigiaPlatform.API.TransitTraceability.Domain.Model.Entities.TelemetryPosition>().AnyAsync(p=>p.RouteId==controlledRouteId) ||
       (await db.Vehicles.SingleAsync()).CurrentPos is not null)throw new Exception("Failed GPS audit left partial route or vehicle position.");
    await TripService(db).ReportAsync("GPS-ATOMIC-TRIP",-12.08,-77.03,gpsTime,20,"company",default);
}
using(var first=new AppDbContext(options))
using(var stale=new AppDbContext(options))
{
    var vehicle=await first.Vehicles.SingleAsync();var staleVehicle=await stale.Vehicles.SingleAsync();
    vehicle.UpdatePosition(new(-12.081,-77.03),gpsTime.AddSeconds(1));await first.SaveChangesAsync();
    staleVehicle.UpdatePosition(new(-12.082,-77.03),gpsTime.AddSeconds(2));
    var failed=false;try {await stale.SaveChangesAsync();}catch(DbUpdateConcurrencyException){failed=true;}
    if(!failed)throw new Exception("Stale GPS vehicle position overwrote a committed report.");
}
Console.WriteLine("PASS: departure and GPS audit failures roll back all business changes; successful retry and stale-position concurrency protection verified.");
// Integration boundary: a legacy name snapshot must never select an arbitrary project.
using (var db = new AppDbContext(options))
{
    var name = "Ambiguous contract project";
    db.Add(new VigiaPlatform.API.ProjectManagement.Domain.Model.ConstructionProject(name,"LIMA","Lima","1","First resident"));
    db.Add(new VigiaPlatform.API.ProjectManagement.Domain.Model.ConstructionProject(name,"LIMA","Lima","2","Second resident"));
    var order = new Order(new VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands.CreateOrderCommand(
        "SOL-990013",name,"Integration Check",
        VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order.OrderPriorityType.Average,
        DateOnly.FromDateTime(DateTime.UtcNow),"Cement",null,
        [new("Cement",2,VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items.ItemsUnitType.u,null)],[]),DateTime.UtcNow);
    order.ChangeStatus(VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order.OrderStatusType.UnderReview,"Integration Check",null,DateTime.UtcNow);
    order.ChangeStatus(VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order.OrderStatusType.Approved,"Integration Check",null,DateTime.UtcNow);
    db.Add(order);await db.SaveChangesAsync();
    var before=await db.Set<Dispatch>().CountAsync();
    var service=new VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration.DispatchPlanningService(db,new UnitOfWork(db,[]));
    var rejected=false;
    try {await service.PlanAsync(new(name,order.Id.ToString(),DateTime.UtcNow.ToString("yyyy-MM-dd"),"18:00","DEPARTURE_TO_WORK","AVERAGE",null),"Integration Check",default);}
    catch(ArgumentException ex) when(ex.Message.Contains("exactly one")){rejected=true;}
    if(!rejected || await db.Set<Dispatch>().CountAsync()!=before)throw new Exception("Ambiguous project planning selected a project or persisted a dispatch.");
}
Console.WriteLine("PASS: duplicate project names reject dispatch planning without a partial dispatch.");

// Stored photo bytes, receipt evidence and audit must commit or roll back together.
var photoUser=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"1"),new Claim(ClaimTypes.Role,"ADMIN"),new Claim(ClaimTypes.Name,"Local evidence check")],"local-check"));
var photoBytes=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg==");
var photoRequest=Guid.NewGuid();int photoReceipt;int evidenceBefore;
using(var db=new AppDbContext(options))
{
 var receipt=new Reception("atomic-photo","ABC-123","Local",1,"Local","Local");db.Add(receipt);await db.SaveChangesAsync();photoReceipt=receipt.Id;
 evidenceBefore=await db.Set<VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities.ReceptionEvidence>().CountAsync();
 await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_photo_audit BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='intentional photo audit failure'");
 var failed=false;
 try{
  using var content=new MemoryStream(photoBytes);var file=new FormFile(content,0,content.Length,"file","local.png"){Headers=new HeaderDictionary(),ContentType="image/png"};
  var service=new VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence.EvidenceFileService(db,new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]),new(db));
  await service.UploadAsync(photoReceipt,file,"Atomic photo",photoRequest,photoUser,default);
 }catch(DbUpdateException){failed=true;}
 finally{await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_photo_audit");}
 if(!failed)throw new Exception("Photo audit failure was not exercised.");
}
using(var db=new AppDbContext(options))
{
 if(await db.Set<VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities.StoredEvidenceFile>().AnyAsync() ||
    await db.Set<VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities.ReceptionEvidence>().CountAsync()!=evidenceBefore)
    throw new Exception("Photo audit failure left bytes or evidence metadata committed.");
 using var content=new MemoryStream(photoBytes);var file=new FormFile(content,0,content.Length,"file","local.png"){Headers=new HeaderDictionary(),ContentType="image/png"};
 var service=new VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence.EvidenceFileService(db,new UnitOfWork(db,[new ActivityChangeRecorder(db,accessor)]),new(db));
 await service.UploadAsync(photoReceipt,file,"Atomic photo",photoRequest,photoUser,default);
}
using(var db=new AppDbContext(options))
{
 var photo=await db.Set<VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities.StoredEvidenceFile>().SingleAsync();
 if(!photo.Content.SequenceEqual(photoBytes)||photo.EvidenceId<=0)throw new Exception("Stored photo retry did not persist complete bytes and linkage.");
}
Console.WriteLine("PASS: photo audit failure rolls back both stored bytes and receipt evidence; exact request retry commits complete linkage.");

// A01 checks: project authorization, authoritative delivery guide, atomic/idempotent closure, concurrency and vehicle release.
int a01SiteId;
int a01DispatchId;
string a01GuideNumber;
string a01AssignedResidentId = "101";
string a01OutsiderResidentId = "202";
VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration.PhysicalReceptionService ReceptionSvc(AppDbContext db) =>
    new(db, new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]));
ClaimsPrincipal MakeResident(string id, string name) => new(new ClaimsIdentity(
    [new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, "SITE_MANAGER"), new Claim(ClaimTypes.Name, name)], "local-check"));

using (var db = new AppDbContext(options))
{
    var project = new VigiaPlatform.API.ProjectManagement.Domain.Model.ConstructionProject("A01 Reception Site", "LIMA", "Lima", a01AssignedResidentId, "Assigned Resident");
    db.Add(project);
    await db.SaveChangesAsync();
    a01SiteId = project.Id;

    var order = new Order(new VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands.CreateOrderCommand(
        "SOL-990014", project.Name, "Assigned Resident",
        VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order.OrderPriorityType.Average,
        DateOnly.FromDateTime(DateTime.UtcNow), "Cement", null,
        [new("Cement", 5, VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items.ItemsUnitType.u, null)], []), DateTime.UtcNow);
    order.ChangeStatus(VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order.OrderStatusType.UnderReview, "Warehouse", null, DateTime.UtcNow);
    order.ChangeStatus(VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order.OrderStatusType.Approved, "Warehouse", null, DateTime.UtcNow);
    db.Add(order);
    await db.SaveChangesAsync();

    var vehicle = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Vehicle("a01-co",
        new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.LicensePlate("A01-999"), "Volvo", "FMX",
        new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.VehicleCapacity(10), "GPS-A01-1");
    var origin = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("a01-co", "a01-wh", "A01 Warehouse",
        VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.Warehouse, fleetLocation, 150);
    var dest = new VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.Aggregates.Geofence("a01-co", a01SiteId.ToString(), "A01 Dest",
        VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects.GeofenceType.JobSite, new(-12.13, -76.98), 150);
    db.AddRange(vehicle, origin, dest);
    await db.SaveChangesAsync();

    var dispatch = new Dispatch(CreateDispatchCommandFromResourceAssembler.ToCommandFromResource(
        new CreateDispatchResource(project.Name, a01SiteId.ToString(), "Lima", DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "18:00", order.Id.ToString(), "DEPARTURE_TO_WORK", "AVERAGE", "Warehouse", "A01-" + Guid.NewGuid().ToString("N"),
            null, [new CreateDispatchItemResource("MAT-A01", "Cement", 5, "u", null)])), DateTime.UtcNow);
    dispatch.ChangeStatus(DispatchStatus.InPreparation, "Warehouse", null, DateTime.UtcNow);
    dispatch.AssignVehicle(vehicle.Id, vehicle.LicensePlate.Value, "Warehouse", DateTime.UtcNow);
    db.Add(dispatch);
    await db.SaveChangesAsync();
    dispatch.RecordNetWeight(100m, 100m, 2m, 1, "Warehouse", DateTimeOffset.UtcNow, Guid.NewGuid());
    await db.SaveChangesAsync();
    dispatch.IssueDeliveryGuide("A01 Driver", "A01 Carrier", 1, "Warehouse", DateTimeOffset.UtcNow, Guid.NewGuid());
    await db.SaveChangesAsync();

    a01DispatchId = dispatch.Id;
    a01GuideNumber = dispatch.DeliveryGuide!.GuideNumber;

    await TripService(db).DepartAsync(a01DispatchId, new(origin.Id, DateTimeOffset.UtcNow.AddHours(1),
        [new(-12.07, -77.03), new(-12.13, -76.98)], Guid.NewGuid()), "Warehouse", "a01-co", default);
}

var assignedUser = MakeResident(a01AssignedResidentId, "Assigned Resident");
var outsiderUser = MakeResident(a01OutsiderResidentId, "Outsider Resident");
var a01Checklist = new VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration.PhysicalChecklist(true, true, true, false, false);
var a01RegReq = Guid.NewGuid();
int a01ReceptionId;

using (var db = new AppDbContext(options))
{
    var svc = ReceptionSvc(db);
    var unauthorized = false;
    try
    {
        await svc.RegisterAsync(new(a01DispatchId.ToString(), a01GuideNumber, "A01 Driver",
            [new("Cement", 5)], a01Checklist, "", Guid.NewGuid()), outsiderUser, default);
    }
    catch (UnauthorizedAccessException) { unauthorized = true; }
    if (!unauthorized) throw new Exception("Unassigned resident was allowed to register another project reception.");

    var wrongGuide = false;
    try
    {
        await svc.RegisterAsync(new(a01DispatchId.ToString(), "GD-FORGED-999", "A01 Driver",
            [new("Cement", 5)], a01Checklist, "", Guid.NewGuid()), assignedUser, default);
    }
    catch (ArgumentException) { wrongGuide = true; }
    if (!wrongGuide) throw new Exception("Reception accepted an invalid delivery guide number.");

    var regPayload = new VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration.PhysicalArrival(
        a01DispatchId.ToString(), a01GuideNumber, "A01 Driver", [new("Cement", 5)], a01Checklist, "", a01RegReq);
    var (created, replayed1) = await svc.RegisterAsync(regPayload, assignedUser, default);
    if (replayed1) throw new Exception("Initial registration unexpectedly reported replay.");
    a01ReceptionId = created.Id;
    var (replayedRec, replayed2) = await svc.RegisterAsync(regPayload, assignedUser, default);
    if (!replayed2 || replayedRec.Id != a01ReceptionId) throw new Exception("Idempotent registration replay failed.");
}

using (var db = new AppDbContext(options))
{
    var svc = ReceptionSvc(db);
    if (await svc.UpdateAsync(a01ReceptionId, new(a01DispatchId.ToString(), a01GuideNumber, "A01 Driver", [new("Cement", 5)], a01Checklist, ""), outsiderUser, default) is not null)
        throw new Exception("Unassigned resident could update another project reception.");
    if (await svc.VerifyAsync(a01ReceptionId, new("VERIFIED_CONFORMANT", a01Checklist, "", Guid.NewGuid()), outsiderUser, default) is not null)
        throw new Exception("Unassigned resident could verify another project reception.");
}

var verifyReq = Guid.NewGuid();
using (var db = new AppDbContext(options))
{
    var svc = ReceptionSvc(db);
    await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_a01_verify BEFORE INSERT ON activity_records FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'intentional a01 verify failure'");
    var failed = false;
    try { await svc.VerifyAsync(a01ReceptionId, new("VERIFIED_CONFORMANT", a01Checklist, "", verifyReq), assignedUser, default); }
    catch (DbUpdateException) { failed = true; }
    finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_a01_verify"); }
    if (!failed) throw new Exception("Forced verification failure did not fail.");
}

using (var db = new AppDbContext(options))
{
    var r = await db.Set<Reception>().SingleAsync(x => x.Id == a01ReceptionId);
    var d = await db.Set<Dispatch>().SingleAsync(x => x.Id == a01DispatchId);
    var t = await db.Set<VigiaPlatform.API.TransitTraceability.Domain.Model.Entities.DispatchTrip>().SingleAsync(x => x.DispatchId == a01DispatchId);
    if (r.ClosedAt is not null || d.Status != DispatchStatus.InTransit || t.ActiveVehicleId is null)
        throw new Exception("Failed verification left partial reception, dispatch or trip state committed.");

    var svc = ReceptionSvc(db);
    var verifyPayload = new VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration.PhysicalVerification(
        "VERIFIED_CONFORMANT", a01Checklist, "", verifyReq);
    var verified = await svc.VerifyAsync(a01ReceptionId, verifyPayload, assignedUser, default)
        ?? throw new Exception("Assigned resident verification returned null.");
    if (verified.Status != VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects.ReceptionStatus.ConformantClosed)
        throw new Exception("Reception did not close conformant.");

    var replayed = await svc.VerifyAsync(a01ReceptionId, verifyPayload, assignedUser, default)
        ?? throw new Exception("Idempotent verify replay returned null.");
    if (replayed.Version != verified.Version)
        throw new Exception("Idempotent verify replay mutated reception version.");
}

using (var db = new AppDbContext(options))
{
    var d = await db.Set<Dispatch>().SingleAsync(x => x.Id == a01DispatchId);
    var t = await db.Set<VigiaPlatform.API.TransitTraceability.Domain.Model.Entities.DispatchTrip>().Include(x => x.Route).SingleAsync(x => x.DispatchId == a01DispatchId);
    if (d.Status != DispatchStatus.Completed || t.ActiveVehicleId is not null || t.Route.IsActive)
        throw new Exception("Conformant reception did not atomically close dispatch and release active trip.");
    var posCountBefore = await db.Set<VigiaPlatform.API.TransitTraceability.Domain.Model.Entities.TelemetryPosition>().CountAsync(p => p.RouteId == t.RouteId);
    await TripService(db).ReportAsync("GPS-A01-1", -12.10, -77.00, DateTimeOffset.UtcNow, 30, "a01-co", default);
    var posCountAfter = await db.Set<VigiaPlatform.API.TransitTraceability.Domain.Model.Entities.TelemetryPosition>().CountAsync(p => p.RouteId == t.RouteId);
    if (posCountAfter != posCountBefore)
        throw new Exception("Late telemetry appended to a trip closed by conformant reception.");
}
Console.WriteLine("PASS: A01 project ownership, authoritative delivery guide, atomic/idempotent conformant closure, and active-trip release verified.");

// A04: Contact requests validation, persistence and idempotency checks
using (var db = new AppDbContext(options))
{
    var contactRepo = new VigiaPlatform.API.ContactRequests.Infrastructure.Persistence.ContactRequestRepository(db);
    var contactUow = new UnitOfWork(db, [new ActivityChangeRecorder(db, accessor)]);
    var contactSvc = new VigiaPlatform.API.ContactRequests.Application.ContactRequestService(contactRepo, contactUow);

    var invalid = await contactSvc.CreateAsync(new(null, "", "", "bad-email", null, "", "UNKNOWN", "short", "es"));
    if (invalid.ValidationErrors is null
        || !invalid.ValidationErrors.ContainsKey("fullName")
        || !invalid.ValidationErrors.ContainsKey("companyName")
        || !invalid.ValidationErrors.ContainsKey("email")
        || !invalid.ValidationErrors.ContainsKey("segment")
        || !invalid.ValidationErrors.ContainsKey("plan")
        || !invalid.ValidationErrors.ContainsKey("message"))
        throw new Exception("A04 contact request validation did not report all missing/invalid fields.");

    var contactReqId = Guid.NewGuid().ToString("D");
    var validCmd = new VigiaPlatform.API.ContactRequests.Application.CreateContactRequestCommand(
        contactReqId, "Carlos Mendoza", "Constructora Andina SAC", "cmendoza@andina.pe",
        "+51 987 654 321", "CONSTRUCTION", "PRO",
        "Solicitamos demostración para controlar despachos y recepciones en tres obras.", "es");
    var created = await contactSvc.CreateAsync(validCmd);
    if (created.Request is null || created.Request.Id <= 0 || created.Request.Status != "RECEIVED")
        throw new Exception("A04 valid contact request was not persisted.");

    var replayed = await contactSvc.CreateAsync(validCmd);
    if (replayed.Request is null || replayed.Request.Id != created.Request.Id)
        throw new Exception("A04 idempotent contact request replay created a duplicate.");

    var conflict = await contactSvc.CreateAsync(validCmd with { CompanyName = "Otra Empresa SAC" });
    if (conflict.ConflictCode != "REQUEST_ID_CONFLICT")
        throw new Exception("A04 reused requestId with different payload did not return REQUEST_ID_CONFLICT.");

    var listed = await contactSvc.ListAsync();
    if (listed.Count != 1 || listed[0].Email != "cmendoza@andina.pe")
        throw new Exception("A04 persisted contact request was not returned by ListAsync.");
}
Console.WriteLine("PASS: A04 contact requests validation, persistence, idempotency and conflict checks verified.");

sealed class InMemoryContactRequestRepository : VigiaPlatform.API.ContactRequests.Domain.Repositories.IContactRequestRepository
{
    private readonly List<VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest> _items = [];
    public Task AddAsync(VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest request, CancellationToken ct = default)
    {
        typeof(VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest)
            .GetProperty("Id")!.SetValue(request, (long)(_items.Count + 1));
        _items.Add(request);
        return Task.CompletedTask;
    }
    public Task<VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest?> FindByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
    public Task<VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest?> FindByRequestIdAsync(string requestId, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.RequestId == requestId));
    public Task<IReadOnlyList<VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VigiaPlatform.API.ContactRequests.Domain.Model.Aggregates.ContactRequest>>(_items.ToList());
}

sealed class NoOpUnitOfWork : VigiaPlatform.API.Shared.Domain.Repositories.IUnitOfWork
{
    public Task CompleteAsync() => Task.CompletedTask;
    public Task CompleteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

