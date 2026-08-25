namespace ECommerce.Application.Pricing;

/// <summary>
/// Server-calculated price breakdown. Never derived from client-supplied totals.
/// Subtotal - Discount + Tax + Shipping = GrandTotal.
/// </summary>
public record PricingResult(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal GrandTotal,
    string? AppliedDiscountCode);
