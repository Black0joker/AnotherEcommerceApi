using ECommerce.Application.Common;
using ECommerce.Application.Features.Cart.GetCart;

namespace ECommerce.Application.Features.Cart.UpdateCartItem;

public record UpdateCartItemCommand(
    Guid ProductId,
    int Quantity
) : ICommand<CartDto>;
