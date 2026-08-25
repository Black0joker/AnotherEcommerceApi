using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.GetUserOrders;

public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal GrandTotal,
    int ItemCount,
    DateTime CreatedAt
);
