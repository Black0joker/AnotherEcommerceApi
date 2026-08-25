using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.GetOrderById;

public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDetailDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<OrderDetailDto>(Error.Unauthorized(
                "Orders.Unauthorized",
                "You must be authenticated to view orders."));
        }

        var order = await _orderRepository.GetWithItemsByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(Error.NotFound(
                "Order.NotFound",
                $"Order with ID '{request.OrderId}' was not found."));
        }

        // IDOR protection: customers can only access their own orders
        if (order.UserId != userId.Value)
        {
            return Result.Failure<OrderDetailDto>(Error.NotFound(
                "Order.NotFound",
                $"Order with ID '{request.OrderId}' was not found."));
        }

        var items = order.Items.Select(i => new OrderItemDto(
            i.ProductId,
            i.ProductName,
            i.SKU,
            i.UnitPrice,
            i.Quantity,
            i.DiscountAmount,
            i.Total
        )).ToList();

        var shippingAddress = order.ShippingAddress is not null
            ? new ShippingAddressDto(
                order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName,
                order.ShippingAddress.StreetLine1,
                order.ShippingAddress.StreetLine2,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country,
                order.ShippingAddress.PhoneNumber)
            : null;

        var dto = new OrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.Subtotal,
            order.DiscountAmount,
            order.TaxAmount,
            order.ShippingAmount,
            order.GrandTotal,
            order.Notes,
            order.CreatedAt,
            order.CancelledAt,
            order.CancellationReason,
            items,
            shippingAddress);

        return Result.Success(dto);
    }
}
