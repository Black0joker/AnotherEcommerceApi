using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Cart : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    // Domain methods
    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
    public int TotalItems => Items.Sum(i => i.Quantity);
    public bool IsEmpty => !Items.Any();
}
