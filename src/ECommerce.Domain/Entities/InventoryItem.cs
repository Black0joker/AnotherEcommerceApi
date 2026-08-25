using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class InventoryItem : AuditableEntity
{
    public Guid ProductId { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation
    public Product Product { get; set; } = null!;
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();

    // Domain methods
    public bool HasAvailableStock(int quantity) => AvailableQuantity >= quantity;

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new Domain.Exceptions.InvalidQuantityException("Reservation quantity must be positive.");
        if (AvailableQuantity < quantity)
            throw new Domain.Exceptions.InsufficientInventoryException($"Cannot reserve {quantity} units. Only {AvailableQuantity} available.");

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new Domain.Exceptions.InvalidQuantityException("Release quantity must be positive.");
        if (ReservedQuantity < quantity)
            throw new Domain.Exceptions.InvalidQuantityException($"Cannot release {quantity} units. Only {ReservedQuantity} reserved.");

        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;
    }

    public void ConfirmReservation(int quantity)
    {
        if (quantity <= 0)
            throw new Domain.Exceptions.InvalidQuantityException("Confirmation quantity must be positive.");
        if (ReservedQuantity < quantity)
            throw new Domain.Exceptions.InvalidQuantityException($"Cannot confirm {quantity} units. Only {ReservedQuantity} reserved.");

        ReservedQuantity -= quantity;
    }

    public void Adjust(int quantity)
    {
        AvailableQuantity += quantity;
        if (AvailableQuantity < 0)
            throw new Domain.Exceptions.InvalidQuantityException("Adjustment would result in negative stock.");
    }
}
