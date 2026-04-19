using Inventory.Application.Products.Common;
using MediatR;

namespace Inventory.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;
