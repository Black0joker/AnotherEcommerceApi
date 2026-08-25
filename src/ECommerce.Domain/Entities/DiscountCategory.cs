namespace ECommerce.Domain.Entities;

public class DiscountCategory
{
    public Guid DiscountId { get; set; }
    public Guid CategoryId { get; set; }

    // Navigation
    public Discount Discount { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
