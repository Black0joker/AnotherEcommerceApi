using ECommerce.Application.Common;
using ECommerce.Application.Features.Wishlist.GetWishlist;

namespace ECommerce.Application.Features.Wishlist.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid ProductId) : ICommand<WishlistDto>;
