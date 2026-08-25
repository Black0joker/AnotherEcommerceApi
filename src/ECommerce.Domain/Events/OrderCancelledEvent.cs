namespace ECommerce.Domain.Events;

public record OrderCancelledEvent(
    Guid OrderId,
    string UserId,
    string OrderNumber,
    string? Reason,
    DateTime OccurredAt
) : IDomainEvent;
