using ECommerce.Application.Common;

namespace ECommerce.Application.Pricing;

/// <summary>
/// Calculates the authoritative order total server-side, applying any discount code.
/// Never trusts totals supplied by the client.
/// </summary>
public interface IDiscountCalculator
{
    Task<Result<PricingResult>> CalculateAsync(
        IReadOnlyList<PricingLine> lines,
        string? discountCode,
        decimal taxAmount = 0m,
        decimal shippingAmount = 0m,
        CancellationToken cancellationToken = default);
}
