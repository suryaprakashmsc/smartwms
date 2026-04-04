using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Repositories;

public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private static readonly IReadOnlyCollection<Product> SeedProducts =
    [
        new Product { Sku = "WMS-001", Name = "Handheld Scanner", QuantityOnHand = 24 },
        new Product { Sku = "WMS-002", Name = "Thermal Label Roll", QuantityOnHand = 180 }
    ];

    public Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SeedProducts);
    }
}
