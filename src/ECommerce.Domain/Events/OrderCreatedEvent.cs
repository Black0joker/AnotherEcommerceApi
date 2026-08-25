namespace ECommerce.Domain.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    string UserId,
    string OrderNumber,
    decimal GrandTotal,
    DateTime OccurredAt
) : IDomainEvent;
