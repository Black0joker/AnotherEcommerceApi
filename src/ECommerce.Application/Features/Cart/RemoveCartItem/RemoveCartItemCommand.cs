using ECommerce.Application.Common;
using ECommerce.Application.Features.Cart.GetCart;

namespace ECommerce.Application.Features.Cart.RemoveCartItem;

public record RemoveCartItemCommand(Guid ProductId) : ICommand<CartDto>;
