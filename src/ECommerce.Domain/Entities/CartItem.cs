using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation
    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;

    // Domain methods
    public decimal LineTotal => UnitPrice * Quantity;
}
