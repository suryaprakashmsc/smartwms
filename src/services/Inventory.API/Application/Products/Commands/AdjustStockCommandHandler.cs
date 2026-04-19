using Inventory.Application.Products.Common;
using Inventory.Domain.Interfaces;
using MediatR;

namespace Inventory.Application.Products.Commands;

public sealed class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public AdjustStockCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");
        }

        product.AdjustStock(request.Delta, request.Reason, request.AdjustedBy);

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return ProductDto.From(product);
    }
}
