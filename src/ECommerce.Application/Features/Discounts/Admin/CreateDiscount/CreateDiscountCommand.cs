using ECommerce.Application.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Discounts.Admin.CreateDiscount;

public record CreateDiscountCommand(
    string Code,
    string Name,
    string? Description,
    DiscountType Type,
    decimal Value,
    DateTime StartsAt,
    DateTime? EndsAt,
    int? UsageLimit,
    decimal? MinimumOrderValue,
    bool IsActive,
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<Guid> CategoryIds) : ICommand<DiscountDto>;
