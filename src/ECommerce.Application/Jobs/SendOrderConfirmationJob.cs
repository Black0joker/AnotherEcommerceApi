namespace ECommerce.Application.Jobs;

/// <summary>
/// Sends an order confirmation email to the customer.
/// </summary>
public record SendOrderConfirmationJob(Guid OrderId) : IJob;
