using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Cart.GetCart;

namespace ECommerce.Application.Features.Cart.AddCartItem;

public class AddCartItemCommandHandler : ICommandHandler<AddCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AddCartItemCommandHandler(
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

    public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<CartDto>(Error.Unauthorized(
                "Cart.Unauthorized",
                "You must be authenticated to manage your cart."));
        }

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

        // Get or create cart
        var cart = await _cartRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        if (cart is null)
        {
            cart = new Domain.Entities.Cart
            {
                UserId = userId.Value,
                Items = new List<Domain.Entities.CartItem>()
            };
            await _cartRepository.AddAsync(cart, cancellationToken);
        }

        // Check if item already exists in cart
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem is not null)
        {
            // Update quantity, respecting inventory
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > availableQuantity)
            {
                return Result.Failure<CartDto>(Error.Validation(
                    "Cart.InsufficientStock",
                    $"Only {availableQuantity} units of this product are available. You already have {existingItem.Quantity} in your cart."));
            }
            existingItem.Quantity = newQuantity;
        }
        else
        {
            // Add new item with server-side price
            cart.Items.Add(new Domain.Entities.CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price // Server-side price - never trust client
            });
        }

        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return updated cart
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
