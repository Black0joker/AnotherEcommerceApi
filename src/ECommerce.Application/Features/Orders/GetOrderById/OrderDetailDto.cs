using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.GetOrderById;

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal GrandTotal,
    string? Notes,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<OrderItemDto> Items,
    ShippingAddressDto? ShippingAddress
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountAmount,
    decimal Total
);

public record ShippingAddressDto(
    string FirstName,
    string LastName,
    string StreetLine1,
    string? StreetLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? PhoneNumber
);
