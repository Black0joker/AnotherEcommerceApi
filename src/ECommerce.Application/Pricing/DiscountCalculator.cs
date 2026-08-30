using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Pricing;

/// <summary>
/// Default discount/price calculator. Validates the discount code and applies
/// percentage/fixed discounts honoring expiration, usage limits, minimum order
/// values, and product/category restrictions.
/// </summary>
public class DiscountCalculator : IDiscountCalculator
{
    private readonly IDiscountRepository _discountRepository;

    public DiscountCalculator(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<Result<PricingResult>> CalculateAsync(
        IReadOnlyList<PricingLine> lines,
        string? discountCode,
        decimal taxAmount = 0m,
        decimal shippingAmount = 0m,
        CancellationToken cancellationToken = default)
    {
        var subtotal = lines.Sum(l => l.LineTotal);

        // No discount requested: straight breakdown.
        if (string.IsNullOrWhiteSpace(discountCode))
        {
            var grandTotal = Normalize(subtotal + taxAmount + shippingAmount);
            return Result.Success(new PricingResult(subtotal, 0m, taxAmount, shippingAmount, grandTotal, null, null));
        }

        var discount = await _discountRepository.GetByCodeAsync(discountCode, cancellationToken);

        if (discount is null)
        {
            return Result.Failure<PricingResult>(Error.Validation(
                "Discount.NotFound",
                $"Discount code '{discountCode}' was not found."));
        }

        if (!discount.IsValid())
        {
            return Result.Failure<PricingResult>(Error.Validation(
                "Discount.Invalid",
                $"Discount code '{discountCode}' is not currently valid."));
        }

        if (!discount.MeetsMinimumOrder(subtotal))
        {
            return Result.Failure<PricingResult>(Error.Validation(
                "Discount.MinimumOrderNotMet",
                $"Discount code '{discountCode}' requires a minimum order of {discount.MinimumOrderValue:C}."));
        }

        // Apply the discount only to eligible lines (product/category restrictions).
        var eligibleSubtotal = lines
            .Where(line => IsLineEligible(line, discount))
            .Sum(line => line.LineTotal);

        var discountAmount = discount.CalculateDiscount(eligibleSubtotal);

        // Never discount below zero and never produce a negative grand total.
        discountAmount = Math.Min(discountAmount, eligibleSubtotal);
        var grandTotal2 = Normalize(subtotal - discountAmount + taxAmount + shippingAmount);

        // Hand the loaded entity back so checkout can increment usage
        // without a second GetByCodeAsync round-trip.
        return Result.Success(new PricingResult(subtotal, discountAmount, taxAmount, shippingAmount, grandTotal2, discount.Code, discount));
    }

    private static bool IsLineEligible(PricingLine line, Discount discount)
    {
        var hasProductRestrictions = discount.DiscountProducts.Count > 0;
        var hasCategoryRestrictions = discount.DiscountCategories.Count > 0;

        // No restrictions: applies to the whole order.
        if (!hasProductRestrictions && !hasCategoryRestrictions)
        {
            return true;
        }

        if (hasProductRestrictions && discount.DiscountProducts.Any(dp => dp.ProductId == line.ProductId))
        {
            return true;
        }

        if (hasCategoryRestrictions && line.CategoryIds.Any(categoryId =>
            discount.DiscountCategories.Any(dc => dc.CategoryId == categoryId)))
        {
            return true;
        }

        return false;
    }

    private static decimal Normalize(decimal amount) => Math.Max(0m, Math.Round(amount, 2));
}
