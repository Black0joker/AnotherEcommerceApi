namespace ECommerce.Application.Features.Inventory.GetInventory;

public record InventoryDto(
    Guid Id,
    Guid ProductId,
    int AvailableQuantity,
    int ReservedQuantity,
    int TotalQuantity,
    DateTime? UpdatedAt
);
