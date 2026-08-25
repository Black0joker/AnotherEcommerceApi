using ECommerce.Application.Common;
using ECommerce.Application.Features.Cart.GetCart;

namespace ECommerce.Application.Features.Cart.AddCartItem;

public record AddCartItemCommand(
    Guid ProductId,
    int Quantity
) : ICommand<CartDto>;
