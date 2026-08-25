using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Wishlist : AuditableEntity
{
    public Guid UserId { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}
