using ECommerce.Application.Common;
using ECommerce.Application.Features.Orders.GetUserOrders;

namespace ECommerce.Application.Features.Orders.Admin.GetAllOrders;

public class GetAllOrdersQuery : PagedQuery, IQuery<PagedResult<OrderSummaryDto>>
{
}
