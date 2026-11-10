using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.Internal.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Application.QueryServices;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Repositories;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration
                           .GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Database connection string is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddScoped<
    IDiscrepancyCaseRepository,
    DiscrepancyCaseRepository>();

builder.Services.AddScoped<
    IDiscrepancyCaseQueryService,
    DiscrepancyCaseQueryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();