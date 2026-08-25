namespace ECommerce.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;
