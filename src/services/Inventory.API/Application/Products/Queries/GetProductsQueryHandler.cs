using Inventory.Application.Products.Common;
using Inventory.Domain.Interfaces;
using MediatR;

namespace Inventory.Application.Products.Queries;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyCollection<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyCollection<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, _) = await _productRepository.GetPagedAsync(
            request.Category,
            request.LowStockOnly,
            request.ActiveOnly,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        return items.Select(ProductDto.From).ToArray();
    }
}
