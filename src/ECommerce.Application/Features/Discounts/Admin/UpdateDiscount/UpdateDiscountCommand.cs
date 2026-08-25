using ECommerce.Application.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Discounts.Admin.UpdateDiscount;

public record UpdateDiscountCommand(
    Guid Id,
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
