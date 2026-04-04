using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Interfaces;
using Warehouse.Infrastructure.Repositories;

namespace Warehouse.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IWarehouseRepository, InMemoryWarehouseRepository>();
        return services;
    }
}
