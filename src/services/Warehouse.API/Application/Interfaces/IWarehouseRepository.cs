using Warehouse.Domain.Entities;

namespace Warehouse.Application.Interfaces;

public interface IWarehouseRepository
{
    Task<IReadOnlyCollection<BinLocation>> GetLocationsAsync(CancellationToken cancellationToken = default);
}
