using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Discounts.Admin.DeleteDiscount;

public record DeleteDiscountCommand(Guid Id) : ICommand<bool>;
