using FluentValidation;
using FluentValidation.Results;
using Inventory.Application.Products.Common;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.ValueObjects;
using MediatR;

namespace Inventory.Application.Products.Commands;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var sku = SKU.Of(request.Sku);

        if (await _productRepository.SkuExistsAsync(sku, ct: cancellationToken))
        {
            throw new ValidationException(
                new[]
                {
                    new ValidationFailure(nameof(request.Sku), $"SKU '{request.Sku}' already exists.")
                });
        }

        var product = Product.Create(
            request.Name,
            request.Sku,
            request.Category,
            request.UnitPrice,
            request.ReorderLevel,
            request.CreatedBy,
            request.Description,
            request.Supplier,
            request.WarehouseLocation,
            request.Barcode,
            request.Currency,
            request.MaximumLevel);

        if (request.InitialQuantity > 0)
        {
            product.AdjustStock(request.InitialQuantity, "Initial stock", request.CreatedBy);
        }

        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return ProductDto.From(product);
    }
}
