using ECommerce.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Development email service that logs emails instead of sending them.
/// Replace with an SMTP/SendGrid implementation in production.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[EMAIL] To: {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
