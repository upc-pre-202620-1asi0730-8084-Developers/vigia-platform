using VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.CommandServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Application.Internal.QueryServices;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Repositories;
using VigiaPlatform.API.FleetAndDeviceRegistry.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Scalar.AspNetCore;
using VigiaPlatform.API.IdentityAndAccessManagement;
using VigiaPlatform.API.ProjectManagement;
using VigiaPlatform.API.InventoryManagement;
using VigiaPlatform.API.ActivityHistory;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Interfaces.Asp.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification;
using VigiaPlatform.API.TransitTraceability;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

const string FrontendCorsPolicy = "VigiaFrontend";

var migrateOnly = args.Contains("--migrate", StringComparer.Ordinal);
var bootstrapAdmin = args.Contains("--bootstrap-admin", StringComparer.Ordinal);
if (migrateOnly && bootstrapAdmin) throw new ArgumentException("Run migration and administrator provisioning separately.");
var builder = WebApplication.CreateBuilder(args.Where(a => a is not ("--migrate" or "--bootstrap-admin")).ToArray());

var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
    builder.Services.AddDataProtection().SetApplicationName("VigiaPlatform")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var proxy = builder.Configuration["ReverseProxy:KnownProxy"];
    if (!string.IsNullOrWhiteSpace(proxy)) options.KnownProxies.Add(IPAddress.Parse(proxy));
});

builder.Services.AddControllers(options =>
    options.Conventions.Add(new KebabCaseRouteNamingConvention()));

builder.Services.AddOpenApi();
builder.Services.AddIdentityAndAccessManagementContext();
builder.Services.AddProjectManagementContext();
builder.Services.AddInventoryManagementContext();
builder.Services.AddActivityHistoryContext();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Reporting.DiscrepancyReportService>();
builder.Services.AddScoped<VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration.PhysicalReceptionService>();
builder.Services.AddScoped<VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Integration.DispatchPlanningService>();

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy, policy => policy
        .WithOrigins(
            "http://localhost:5173",
            "https://vigia-frontend-8084.web.app")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Transit Traceability
builder.Services.AddTransitTraceabilityContext();
builder.Services.AddSiteReceptionAndVerificationContext();

// Discrepancy and Evidence Management
builder.Services.AddScoped<
    IDiscrepancyCaseRepository,
    DiscrepancyCaseRepository>();

builder.Services.AddScoped<
    IDiscrepancyCaseQueryService,
    DiscrepancyCaseQueryService>();

builder.Services.AddScoped<
    IDiscrepancyCaseCommandService,
    DiscrepancyCaseCommandService>();

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IGeofenceRepository, GeofenceRepository>();
builder.Services.AddScoped<IVehicleCommandService, VehicleCommandService>();
builder.Services.AddScoped<IGeofenceCommandService, GeofenceCommandService>();
builder.Services.AddScoped<IVehicleQueryService, VehicleQueryService>();
builder.Services.AddScoped<IGeofenceQueryService, GeofenceQueryService>();

builder.Services.AddScoped<IDispatchRepository, DispatchRepository>();
builder.Services.AddScoped<IDispatchCommandService, DispatchCommandService>();
builder.Services.AddScoped<IDispatchQueryService, DispatchQueryService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();

var app = builder.Build();

if (migrateOnly || bootstrapAdmin)
{
    using var scope = app.Services.CreateScope();
    if (migrateOnly)
        await DatabaseMigrationRunner.MigrateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    else
        await VigiaPlatform.API.IdentityAndAccessManagement.Infrastructure.Operations.AdministratorProvisioning.RunAsync(
            scope.ServiceProvider, builder.Configuration);
    return;
}

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (builder.Configuration.GetValue<bool>("Development:SeedDemoData") &&
        !dbContext.Database.GetDbConnection().Database.StartsWith("vigia_local_", StringComparison.Ordinal))
        throw new InvalidOperationException("Demo setup requires a separate database prefixed vigia_local_.");
    await DatabaseMigrationRunner.MigrateAsync(dbContext);
    await VigiaPlatform.API.Shared.Infrastructure.Development.LocalIntegrationSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "healthy" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception) when (!ct.IsCancellationRequested)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.Run();
