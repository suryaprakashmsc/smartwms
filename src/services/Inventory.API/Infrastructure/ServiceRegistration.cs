using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<InventoryDbContext>(opts =>
            opts.UseNpgsql(
                connectionString,
                builder => builder.MigrationsAssembly(typeof(ServiceRegistration).Assembly.GetName().Name)));

        services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }
}
