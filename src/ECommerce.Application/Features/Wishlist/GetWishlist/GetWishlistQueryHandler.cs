using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Wishlist.GetWishlist;

public class GetWishlistQueryHandler : IQueryHandler<GetWishlistQuery, WishlistDto>
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetWishlistQueryHandler(
        IWishlistRepository wishlistRepository,
        ICurrentUserService currentUserService)
    {
        _wishlistRepository = wishlistRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WishlistDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<WishlistDto>(Error.Unauthorized(
                "Wishlist.Unauthorized",
                "You must be authenticated to view your wishlist."));
        }

        var wishlist = await _wishlistRepository.GetWithItemsByUserIdAsync(userId.Value, cancellationToken);

        if (wishlist is null)
        {
            return Result.Success(new WishlistDto(
                Guid.Empty,
                new List<WishlistItemDto>(),
                0));
        }

        var items = wishlist.Items.Select(i => new WishlistItemDto(
            i.ProductId,
            i.Product?.Name ?? "",
            i.Product?.Slug ?? "",
            i.Product?.Price ?? 0,
            i.Product?.IsActive ?? false,
            i.CreatedAt
        )).ToList();

        var dto = new WishlistDto(
            wishlist.Id,
            items,
            items.Count);

        return Result.Success(dto);
    }
}
