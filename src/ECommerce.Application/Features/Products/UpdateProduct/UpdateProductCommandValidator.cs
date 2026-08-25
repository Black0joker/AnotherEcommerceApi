using FluentValidation;

namespace ECommerce.Application.Features.Products.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Product description is required.")
            .MaximumLength(4000).WithMessage("Product description must not exceed 4000 characters.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU must not exceed 100 characters.")
            .Matches(@"^[A-Za-z0-9-]+$")
            .WithMessage("SKU must contain only alphanumeric characters and hyphens.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(0)
            .When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare-at price must be greater than zero.");
    }
}
