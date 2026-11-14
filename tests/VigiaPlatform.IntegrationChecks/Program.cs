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
Console.WriteLine($"PASS: complete migration chain, real MySQL rollback, atomic success and telemetry history. Isolated database: {connection.Database}");
