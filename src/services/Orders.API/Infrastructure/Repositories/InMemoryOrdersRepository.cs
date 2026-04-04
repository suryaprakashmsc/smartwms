using Orders.Application.Interfaces;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Repositories;

public sealed class InMemoryOrdersRepository : IOrdersRepository
{
    private static readonly IReadOnlyCollection<PurchaseOrder> SeedOrders =
    [
        new PurchaseOrder { OrderNumber = "PO-1001", SupplierName = "Northwind Parts", Status = "Draft" },
        new PurchaseOrder { OrderNumber = "PO-1002", SupplierName = "Global Packing Co", Status = "Approved" }
    ];

    public Task<IReadOnlyCollection<PurchaseOrder>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SeedOrders);
    }
}
