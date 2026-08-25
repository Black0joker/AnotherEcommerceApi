using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Discounts.Admin.GetDiscounts;

public record GetDiscountsQuery : IQuery<IReadOnlyList<DiscountDto>>;
