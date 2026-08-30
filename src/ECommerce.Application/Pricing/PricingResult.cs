using ECommerce.Domain.Entities;

namespace ECommerce.Application.Pricing;

/// <summary>
/// Server-calculated price breakdown. Never derived from client-supplied totals.
/// Subtotal - Discount + Tax + Shipping = GrandTotal.
/// AppliedDiscount carries the already-loaded discount entity so callers that
/// need to record usage can reuse it instead of fetching by code again.
/// </summary>
public record PricingResult(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal GrandTotal,
    string? AppliedDiscountCode,
    Discount? AppliedDiscount = null);
