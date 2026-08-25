using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Wishlist : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}
