using ECommerce.Application.Abstractions;
using ECommerce.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs.Handlers;

public class SendOrderConfirmationJobHandler : IJobHandler<SendOrderConfirmationJob>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendOrderConfirmationJobHandler> _logger;

    public SendOrderConfirmationJobHandler(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<SendOrderConfirmationJobHandler> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(SendOrderConfirmationJob job, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetWithItemsByIdAsync(job.OrderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Order confirmation skipped: order {OrderId} not found", job.OrderId);
            return;
        }

        var user = await _userRepository.GetByIdAsync(order.UserId, cancellationToken);

        if (user?.Email is null)
        {
            _logger.LogWarning("Order confirmation skipped: user email not found for order {OrderId}", job.OrderId);
            return;
        }

        var body = BuildEmailBody(order.OrderNumber, order.GrandTotal, order.Items.Count);

        await _emailService.SendAsync(
            user.Email,
            $"Order Confirmation - {order.OrderNumber}",
            body,
            cancellationToken);

        _logger.LogInformation("Sent order confirmation for {OrderNumber} to {Email}", order.OrderNumber, user.Email);
    }

    private static string BuildEmailBody(string orderNumber, decimal grandTotal, int itemCount)
    {
        return $"Thank you for your order!\n\n" +
               $"Order Number: {orderNumber}\n" +
               $"Items: {itemCount}\n" +
               $"Total: {grandTotal:C}\n\n" +
               $"We will notify you when your order ships.";
    }
}
