namespace ECommerce.Domain.Exceptions;

public class InvalidQuantityException : DomainException
{
    public InvalidQuantityException(string message) : base(message) { }
}
