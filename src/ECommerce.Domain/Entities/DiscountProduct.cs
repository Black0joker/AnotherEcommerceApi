namespace ECommerce.Domain.Entities;

public class DiscountProduct
{
    public Guid DiscountId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation
    public Discount Discount { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
