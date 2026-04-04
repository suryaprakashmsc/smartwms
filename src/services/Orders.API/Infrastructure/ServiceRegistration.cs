using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Interfaces;
using Orders.Infrastructure.Repositories;

namespace Orders.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOrdersRepository, InMemoryOrdersRepository>();
        return services;
    }
}
