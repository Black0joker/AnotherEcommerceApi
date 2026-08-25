using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Orders.GetUserOrders;

namespace ECommerce.Application.Features.Orders.Admin.GetAllOrders;

public class GetAllOrdersQueryHandler : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<IReadOnlyList<OrderSummaryDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);

        var dtos = orders.Select(o => new OrderSummaryDto(
            o.Id,
            o.OrderNumber,
            o.Status,
            o.GrandTotal,
            o.Items.Count,
            o.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<OrderSummaryDto>>(dtos);
    }
}
