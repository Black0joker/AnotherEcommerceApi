using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Discounts;

public static class DiscountMapper
{
    public static DiscountDto ToDto(Discount discount)
    {
        return new DiscountDto(
            discount.Id,
            discount.Code,
            discount.Name,
            discount.Description,
            discount.Type,
            discount.Value,
            discount.StartsAt,
            discount.EndsAt,
            discount.UsageLimit,
            discount.UsedCount,
            discount.MinimumOrderValue,
            discount.IsActive,
            discount.DiscountProducts.Select(dp => dp.ProductId).ToList(),
            discount.DiscountCategories.Select(dc => dc.CategoryId).ToList(),
            discount.CreatedAt);
    }
}
