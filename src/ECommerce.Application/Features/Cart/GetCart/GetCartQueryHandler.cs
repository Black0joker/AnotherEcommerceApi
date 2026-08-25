using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Cart.GetCart;

public class GetCartQueryHandler : IQueryHandler<GetCartQuery, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCartQueryHandler(
        ICartRepository cartRepository,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<CartDto>(Error.Unauthorized(
                "Cart.Unauthorized",
                "You must be authenticated to view your cart."));
        }

        var cart = await _cartRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        if (cart is null)
        {
            // Return empty cart if none exists
            return Result.Success(new CartDto(
                Guid.Empty,
                new List<CartItemDto>(),
                0,
                DateTime.UtcNow));
        }

        var items = cart.Items.Select(i => new CartItemDto(
            i.ProductId,
            i.Product?.Name ?? "",
            i.Product?.Slug ?? "",
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice
        )).ToList();

        var dto = new CartDto(
            cart.Id,
            items,
            items.Sum(i => i.LineTotal),
            cart.UpdatedAt);

        return Result.Success(dto);
    }
}
