using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class OrderAddress : BaseEntity
{
    public Guid OrderId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StreetLine1 { get; set; } = string.Empty;
    public string? StreetLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
