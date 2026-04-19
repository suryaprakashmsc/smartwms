using Inventory.Application.Products.Common;
using MediatR;

namespace Inventory.Application.Products.Queries;

public sealed record GetProductsQuery(
    string? Category = null,
    bool? LowStockOnly = null,
    bool? ActiveOnly = true,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50) : IRequest<IReadOnlyCollection<ProductDto>>;
