using ECommerce.Application.Common;
using ECommerce.Application.Features.Wishlist.GetWishlist;

namespace ECommerce.Application.Features.Wishlist.AddToWishlist;

public record AddToWishlistCommand(Guid ProductId) : ICommand<WishlistDto>;
