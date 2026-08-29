using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Pricing;

namespace ECommerce.Application.Features.Discounts.ValidateDiscountCode;

public class ValidateDiscountCodeQueryHandler : IQueryHandler<ValidateDiscountCodeQuery, PricingResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDiscountCalculator _discountCalculator;

    public ValidateDiscountCodeQueryHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IDiscountCalculator discountCalculator)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _discountCalculator = discountCalculator;
    }

    public async Task<Result<PricingResult>> Handle(ValidateDiscountCodeQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<PricingResult>(Error.Unauthorized(
                "Discount.Unauthorized",
                "You must be authenticated to validate a discount code."));
        }

        var cart = await _cartRepository.GetWithItemsByUserIdAsync(userId.Value, cancellationToken);
        if (cart is null || cart.IsEmpty)
        {
            return Result.Failure<PricingResult>(Error.Validation(
                "Discount.EmptyCart",
                "Your cart is empty. Add items before validating a discount code."));
        }

        // Build authoritative pricing lines from server-side product data.
        var lines = new List<PricingLine>();
        foreach (var cartItem in cart.Items)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(cartItem.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
            {
                continue;
            }

            var categoryIds = product.ProductCategories.Select(pc => pc.CategoryId).ToList();
            lines.Add(new PricingLine(product.Id, categoryIds, product.Price, cartItem.Quantity));
        }

        return await _discountCalculator.CalculateAsync(lines, request.Code, 0m, 0m, cancellationToken);
    }
}
