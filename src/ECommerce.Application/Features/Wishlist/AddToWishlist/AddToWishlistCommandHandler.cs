using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Wishlist.GetWishlist;

namespace ECommerce.Application.Features.Wishlist.AddToWishlist;

public class AddToWishlistCommandHandler : ICommandHandler<AddToWishlistCommand, WishlistDto>
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AddToWishlistCommandHandler(
        IWishlistRepository wishlistRepository,
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDto>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<WishlistDto>(Error.Unauthorized(
                "Wishlist.Unauthorized",
                "You must be authenticated to manage your wishlist."));
        }

        // Validate product exists and is active
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return Result.Failure<WishlistDto>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.ProductId}' was not found or is not available."));
        }

        // Get or create wishlist
        var wishlist = await _wishlistRepository.GetWithItemsByUserIdAsync(userId.Value, cancellationToken);

        if (wishlist is null)
        {
            wishlist = new Domain.Entities.Wishlist
            {
                UserId = userId.Value,
                Items = new List<Domain.Entities.WishlistItem>()
            };
            await _wishlistRepository.AddAsync(wishlist, cancellationToken);
        }

        // Check if item already exists (unique constraint)
        var existingItem = wishlist.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem is not null)
        {
            return Result.Failure<WishlistDto>(Error.Conflict(
                "Wishlist.AlreadyExists",
                "This product is already in your wishlist."));
        }

        // Add new item
        wishlist.Items.Add(new Domain.Entities.WishlistItem
        {
            WishlistId = wishlist.Id,
            ProductId = request.ProductId
        });

        _wishlistRepository.Update(wishlist);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return updated wishlist
        var items = wishlist.Items.Select(i => new WishlistItemDto(
            i.ProductId,
            i.Product?.Name ?? product.Name,
            i.Product?.Slug ?? product.Slug,
            i.Product?.Price ?? product.Price,
            i.Product?.IsActive ?? product.IsActive,
            i.CreatedAt
        )).ToList();

        var dto = new WishlistDto(wishlist.Id, items, items.Count);
        return Result.Success(dto);
    }
}
