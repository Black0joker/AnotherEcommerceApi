using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Inventory.GetInventoryTransactions;

public record InventoryTransactionDto(
    Guid Id,
    Guid ProductId,
    InventoryTransactionType Type,
    int Quantity,
    string? Reference,
    string? Notes,
    DateTime CreatedAt
);
