using Inventory.Application.Products.Common;
using MediatR;

namespace Inventory.Application.Products.Commands;

public sealed record AdjustStockCommand(
    Guid ProductId,
    int Delta,
    string Reason,
    string AdjustedBy) : IRequest<ProductDto>;
