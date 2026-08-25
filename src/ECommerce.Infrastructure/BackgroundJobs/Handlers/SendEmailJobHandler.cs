using ECommerce.Application.Abstractions;
using ECommerce.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs.Handlers;

public class SendEmailJobHandler : IJobHandler<SendEmailJob>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailJobHandler> _logger;

    public SendEmailJobHandler(IEmailService emailService, ILogger<SendEmailJobHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(SendEmailJob job, CancellationToken cancellationToken = default)
    {
        await _emailService.SendAsync(job.To, job.Subject, job.Body, cancellationToken);
        _logger.LogInformation("Sent email to {To} with subject '{Subject}'", job.To, job.Subject);
    }
}
