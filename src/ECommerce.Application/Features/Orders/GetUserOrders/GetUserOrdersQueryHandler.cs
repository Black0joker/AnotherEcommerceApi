using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.GetUserOrders;

public class GetUserOrdersQueryHandler : IQueryHandler<GetUserOrdersQuery, PagedResult<OrderSummaryDto>>
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

    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<PagedResult<OrderSummaryDto>>(Error.Unauthorized(
                "Orders.Unauthorized",
                "You must be authenticated to view orders."));
        }

        var (orders, totalCount) = await _orderRepository.GetByUserIdPagedAsync(
            userId.Value,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = orders.Select(o => new OrderSummaryDto(
            o.Id,
            o.OrderNumber,
            o.Status,
            o.GrandTotal,
            o.Items.Count,
            o.CreatedAt
        )).ToList();

        var result = new PagedResult<OrderSummaryDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
