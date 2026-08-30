using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.GetUserOrders;

public class GetUserOrdersQuery : PagedQuery, IQuery<PagedResult<OrderSummaryDto>>
{
}
