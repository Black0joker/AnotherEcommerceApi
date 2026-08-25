using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
