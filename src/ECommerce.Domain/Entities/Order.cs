using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Order : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Notes { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public OrderAddress? ShippingAddress { get; set; }
    public Payment? Payment { get; set; }

    // Domain methods
    public bool CanCancel => Status is OrderStatus.Pending or OrderStatus.Confirmed;

    public void Cancel(string? reason = null)
    {
        if (!CanCancel)
            throw new Exceptions.BusinessRuleViolationException(
                $"Order cannot be cancelled in '{Status}' status. Only Pending or Confirmed orders can be cancelled.");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public void TransitionTo(OrderStatus newStatus)
    {
        ValidateTransition(newStatus);
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ValidateTransition(OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new[] { OrderStatus.Shipped },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
            [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        if (!validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new Exceptions.BusinessRuleViolationException(
                $"Invalid order status transition from '{Status}' to '{newStatus}'.");
        }
    }
}
