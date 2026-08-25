using ECommerce.Application.Common;
using ECommerce.Application.Pricing;

namespace ECommerce.Application.Features.Discounts.ValidateDiscountCode;

public record ValidateDiscountCodeQuery(string Code) : IQuery<PricingResult>;
