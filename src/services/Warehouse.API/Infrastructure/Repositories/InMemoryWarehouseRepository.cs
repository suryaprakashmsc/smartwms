using Warehouse.Application.Interfaces;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Repositories;

public sealed class InMemoryWarehouseRepository : IWarehouseRepository
{
    private static readonly IReadOnlyCollection<BinLocation> SeedLocations =
    [
        new BinLocation { Code = "A-01-01", Zone = "A" },
        new BinLocation { Code = "B-02-03", Zone = "B", IsAvailable = false }
    ];

    public Task<IReadOnlyCollection<BinLocation>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SeedLocations);
    }
}
