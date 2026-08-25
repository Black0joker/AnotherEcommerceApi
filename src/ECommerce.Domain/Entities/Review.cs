using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Review : AuditableEntity
{
    public Guid ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    // Domain methods
    public static void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new Exceptions.BusinessRuleViolationException("Rating must be between 1 and 5.");
    }
}
