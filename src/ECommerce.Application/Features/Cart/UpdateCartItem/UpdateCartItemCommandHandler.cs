using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Cart.GetCart;

namespace ECommerce.Application.Features.Cart.UpdateCartItem;

public class UpdateCartItemCommandHandler : ICommandHandler<UpdateCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCartItemCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartDto>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
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

        // If quantity is 0, remove the item
        if (request.Quantity <= 0)
        {
            cart.Items.Remove(cartItem);
        }
        else
        {
            // Validate product exists and is active
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
            {
                return Result.Failure<CartDto>(Error.NotFound(
                    "Product.NotFound",
                    $"Product with ID '{request.ProductId}' was not found or is not available."));
            }

            // Validate inventory
            var availableQuantity = product.InventoryItem?.AvailableQuantity ?? 0;
            if (request.Quantity > availableQuantity)
            {
                return Result.Failure<CartDto>(Error.Validation(
                    "Cart.InsufficientStock",
                    $"Only {availableQuantity} units of this product are available."));
            }

            cartItem.Quantity = request.Quantity;
        }

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
