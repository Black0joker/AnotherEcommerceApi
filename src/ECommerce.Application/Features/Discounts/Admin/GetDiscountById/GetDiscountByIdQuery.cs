using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Discounts.Admin.GetDiscountById;

public record GetDiscountByIdQuery(Guid Id) : IQuery<DiscountDto>;
