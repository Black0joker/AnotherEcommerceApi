using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Wishlist.GetWishlist;

public record GetWishlistQuery() : IQuery<WishlistDto>;
