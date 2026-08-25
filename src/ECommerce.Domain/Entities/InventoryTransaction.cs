using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class InventoryTransaction : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid ProductId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
