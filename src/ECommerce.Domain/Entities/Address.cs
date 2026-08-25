using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Address : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StreetLine1 { get; set; } = string.Empty;
    public string? StreetLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsDefault { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
