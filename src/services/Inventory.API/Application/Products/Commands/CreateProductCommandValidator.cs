using FluentValidation;

namespace Inventory.Application.Products.Commands;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z]{2,6}-[A-Za-z0-9]{2,10}$");

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.InitialQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.MaximumLevel)
            .GreaterThanOrEqualTo(x => x.ReorderLevel)
            .When(x => x.MaximumLevel.HasValue);
    }
}
