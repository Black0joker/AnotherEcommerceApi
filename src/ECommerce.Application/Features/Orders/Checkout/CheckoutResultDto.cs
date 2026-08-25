using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.Checkout;

public record CheckoutResultDto(
    Guid OrderId,
    string OrderNumber,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal GrandTotal,
    int ItemCount,
    DateTime CreatedAt
);
