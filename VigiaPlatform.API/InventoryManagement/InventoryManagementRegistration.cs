namespace VigiaPlatform.API.InventoryManagement;

using VigiaPlatform.API.InventoryManagement.Application;
using VigiaPlatform.API.InventoryManagement.Domain.Repositories;
using VigiaPlatform.API.InventoryManagement.Infrastructure.Persistence;

public static class InventoryManagementRegistration
{
    public static IServiceCollection AddInventoryManagementContext(this IServiceCollection services)
    {
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<InventoryQueryService>();
        return services;
    }
}
