using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        return services;
    }
}
