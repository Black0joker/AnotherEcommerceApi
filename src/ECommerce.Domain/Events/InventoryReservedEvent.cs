namespace ECommerce.Domain.Events;

public record InventoryReservedEvent(
    Guid ProductId,
    int Quantity,
    string? Reference,
    DateTime OccurredAt
) : IDomainEvent;
