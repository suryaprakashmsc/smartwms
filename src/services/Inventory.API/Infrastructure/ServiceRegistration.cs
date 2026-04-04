using Inventory.Application.Interfaces;
using Inventory.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IInventoryRepository, InMemoryInventoryRepository>();
        return services;
    }
}
