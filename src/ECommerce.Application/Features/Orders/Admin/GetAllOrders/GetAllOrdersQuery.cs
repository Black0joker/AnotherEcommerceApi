using ECommerce.Application.Common;
using ECommerce.Application.Features.Orders.GetUserOrders;

namespace ECommerce.Application.Features.Orders.Admin.GetAllOrders;

public record GetAllOrdersQuery() : IQuery<IReadOnlyList<OrderSummaryDto>>;
