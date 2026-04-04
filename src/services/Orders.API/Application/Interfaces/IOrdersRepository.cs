using Orders.Domain.Entities;

namespace Orders.Application.Interfaces;

public interface IOrdersRepository
{
    Task<IReadOnlyCollection<PurchaseOrder>> GetOrdersAsync(CancellationToken cancellationToken = default);
}
