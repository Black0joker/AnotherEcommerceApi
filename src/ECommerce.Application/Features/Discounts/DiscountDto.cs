using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Discounts;

public record DiscountDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DiscountType Type,
    decimal Value,
    DateTime StartsAt,
    DateTime? EndsAt,
    int? UsageLimit,
    int UsedCount,
    decimal? MinimumOrderValue,
    bool IsActive,
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<Guid> CategoryIds,
    DateTime CreatedAt);
