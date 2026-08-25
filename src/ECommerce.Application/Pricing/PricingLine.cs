namespace ECommerce.Application.Pricing;

/// <summary>
/// A single line used for server-side price calculation.
/// </summary>
public record PricingLine(
    Guid ProductId,
    IReadOnlyList<Guid> CategoryIds,
    decimal UnitPrice,
    int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
