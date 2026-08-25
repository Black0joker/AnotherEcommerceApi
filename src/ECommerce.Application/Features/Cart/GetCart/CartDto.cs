namespace ECommerce.Application.Features.Cart.GetCart;

public record CartDto(
    Guid Id,
    IReadOnlyList<CartItemDto> Items,
    decimal Total,
    DateTime? UpdatedAt
);

public record CartItemDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
