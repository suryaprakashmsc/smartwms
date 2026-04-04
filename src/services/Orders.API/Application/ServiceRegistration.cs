using Microsoft.Extensions.DependencyInjection;

namespace Orders.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        return services;
    }
}
