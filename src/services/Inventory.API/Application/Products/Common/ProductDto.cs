using Inventory.Domain.Entities;

namespace Inventory.Application.Products.Common;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    string Category,
    decimal UnitPrice,
    string Currency,
    int QuantityOnHand,
    int ReorderLevel,
    int? MaximumLevel,
    bool IsLowStock,
    bool IsActive
)
{
    public static ProductDto From(Product product) =>
        new(
            product.Id,
            product.SKU.Value,
            product.Name,
            product.Description,
            product.Category,
            product.UnitPrice.Amount,
            product.UnitPrice.Currency,
            product.Stock.Quantity,
            product.Stock.ReorderLevel,
            product.Stock.MaximumLevel,
            product.IsLowStock,
            product.IsActive);
}
