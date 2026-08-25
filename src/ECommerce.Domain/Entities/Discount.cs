using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Discount : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<DiscountProduct> DiscountProducts { get; set; } = new List<DiscountProduct>();
    public ICollection<DiscountCategory> DiscountCategories { get; set; } = new List<DiscountCategory>();

    // Domain methods
    public bool IsValid(DateTime? orderDate = null)
    {
        var date = orderDate ?? DateTime.UtcNow;

        if (!IsActive) return false;
        if (date < StartsAt) return false;
        if (EndsAt.HasValue && date > EndsAt.Value) return false;
        if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value) return false;

        return true;
    }

    public bool MeetsMinimumOrder(decimal orderSubtotal)
    {
        if (!MinimumOrderValue.HasValue) return true;
        return orderSubtotal >= MinimumOrderValue.Value;
    }

    public decimal CalculateDiscount(decimal amount)
    {
        return Type switch
        {
            DiscountType.Percentage => amount * (Value / 100m),
            DiscountType.Fixed => Math.Min(Value, amount),
            _ => 0m
        };
    }

    public void IncrementUsage()
    {
        UsedCount++;
    }
}
