using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Cart.GetCart;

namespace ECommerce.Application.Features.Cart.RemoveCartItem;

public class RemoveCartItemCommandHandler : ICommandHandler<RemoveCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCartItemCommandHandler(
        ICartRepository cartRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<CartDto>(Error.Unauthorized(
                "Cart.Unauthorized",
                "You must be authenticated to manage your cart."));
        }

        var cart = await _cartRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        if (cart is null)
        {
            return Result.Failure<CartDto>(Error.NotFound(
                "Cart.NotFound",
                "Cart not found."));
        }

        var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (cartItem is null)
        {
            return Result.Failure<CartDto>(Error.NotFound(
                "Cart.ItemNotFound",
                $"Product with ID '{request.ProductId}' is not in your cart."));
        }

        cart.Items.Remove(cartItem);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(cart);
        return Result.Success(dto);
    }

    private static CartDto MapToDto(Domain.Entities.Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemDto(
            i.ProductId,
            i.Product?.Name ?? "",
            i.Product?.Slug ?? "",
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice
        )).ToList();

        return new CartDto(
            cart.Id,
            items,
            items.Sum(i => i.LineTotal),
            cart.UpdatedAt);
    }
}
