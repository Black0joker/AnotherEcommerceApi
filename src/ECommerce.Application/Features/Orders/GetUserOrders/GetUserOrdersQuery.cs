using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.GetUserOrders;

public record GetUserOrdersQuery() : IQuery<IReadOnlyList<OrderSummaryDto>>;
