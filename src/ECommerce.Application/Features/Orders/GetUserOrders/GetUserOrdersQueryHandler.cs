using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.GetUserOrders;

public class GetUserOrdersQueryHandler : IQueryHandler<GetUserOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUserOrdersQueryHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<OrderSummaryDto>>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<IReadOnlyList<OrderSummaryDto>>(Error.Unauthorized(
                "Orders.Unauthorized",
                "You must be authenticated to view orders."));
        }

        var orders = await _orderRepository.GetByUserIdAsync(userId.Value, cancellationToken);

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
