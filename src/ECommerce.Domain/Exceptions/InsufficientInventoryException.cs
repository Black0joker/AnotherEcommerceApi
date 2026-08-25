namespace ECommerce.Domain.Exceptions;

public class InsufficientInventoryException : DomainException
{
    public InsufficientInventoryException(string message) : base(message) { }
}
