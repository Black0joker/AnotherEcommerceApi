namespace ECommerce.Application.Features.Wishlist.GetWishlist;

public record WishlistDto(
    Guid Id,
    IReadOnlyList<WishlistItemDto> Items,
    int TotalItems
);

public record WishlistItemDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    decimal ProductPrice,
    bool IsProductActive,
    DateTime AddedAt
);
