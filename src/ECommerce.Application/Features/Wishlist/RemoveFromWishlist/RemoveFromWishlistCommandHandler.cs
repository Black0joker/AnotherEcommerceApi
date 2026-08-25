using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Wishlist.GetWishlist;

namespace ECommerce.Application.Features.Wishlist.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler : ICommandHandler<RemoveFromWishlistCommand, WishlistDto>
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromWishlistCommandHandler(
        IWishlistRepository wishlistRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDto>> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<WishlistDto>(Error.Unauthorized(
                "Wishlist.Unauthorized",
                "You must be authenticated to manage your wishlist."));
        }

        var wishlist = await _wishlistRepository.GetWithItemsByUserIdAsync(userId.Value, cancellationToken);
        if (wishlist is null)
        {
            return Result.Failure<WishlistDto>(Error.NotFound(
                "Wishlist.NotFound",
                "Wishlist not found."));
        }

        var item = wishlist.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item is null)
        {
            return Result.Failure<WishlistDto>(Error.NotFound(
                "Wishlist.ItemNotFound",
                $"Product with ID '{request.ProductId}' is not in your wishlist."));
        }

        wishlist.Items.Remove(item);
        _wishlistRepository.Update(wishlist);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var items = wishlist.Items.Select(i => new WishlistItemDto(
            i.ProductId,
            i.Product?.Name ?? "",
            i.Product?.Slug ?? "",
            i.Product?.Price ?? 0,
            i.Product?.IsActive ?? false,
            i.CreatedAt
        )).ToList();

        var dto = new WishlistDto(wishlist.Id, items, items.Count);
        return Result.Success(dto);
    }
}
