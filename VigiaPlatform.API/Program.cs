using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Order_management_and_distpatch.Application.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.CommandServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.Internal.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Application.QueryServices;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Repositories;
using VigiaPlatform.API.Order_management_and_distpatch.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
    if (builder.Environment.IsDevelopment())
        options.LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
});

// Shared
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Order management and dispatch bounded context
builder.Services.AddScoped<IDispatchRepository, DispatchRepository>();
builder.Services.AddScoped<IDispatchCommandService, DispatchCommandService>();
builder.Services.AddScoped<IDispatchQueryService, DispatchQueryService>();

var app = builder.Build();

// Make sure the database schema exists
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
