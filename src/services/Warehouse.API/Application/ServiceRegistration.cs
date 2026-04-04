using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouseApplication(this IServiceCollection services)
    {
        return services;
    }
}
