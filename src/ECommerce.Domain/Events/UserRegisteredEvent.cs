namespace ECommerce.Domain.Events;

public record UserRegisteredEvent(
    string UserId,
    string Email,
    DateTime OccurredAt
) : IDomainEvent;
