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
using Scalar.AspNetCore;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Interfaces.Asp.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.SiteReceptionAndVerification;
using VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Persistence.EntityFrameworkCore.Seeding;
using VigiaPlatform.API.TransitTraceability;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.CommandServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

const string FrontendCorsPolicy = "VigiaFrontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    options.Conventions.Add(new KebabCaseRouteNamingConvention()));

builder.Services.AddOpenApi();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    SiteReceptionSeeder.Seed(dbContext);
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
