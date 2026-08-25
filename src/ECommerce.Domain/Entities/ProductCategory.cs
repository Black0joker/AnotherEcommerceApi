namespace ECommerce.Domain.Entities;

public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Guid CategoryId { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
