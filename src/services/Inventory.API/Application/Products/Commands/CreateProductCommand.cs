using Inventory.Application.Products.Common;
using MediatR;

namespace Inventory.Application.Products.Commands;

public sealed record CreateProductCommand(
    string Name,
    string Sku,
    string Category,
    decimal UnitPrice,
    int ReorderLevel,
    int InitialQuantity,
    string CreatedBy,
    string? Description = null,
    string? Supplier = null,
    string? WarehouseLocation = null,
    string? Barcode = null,
    string Currency = "USD",
    int? MaximumLevel = null) : IRequest<ProductDto>;
