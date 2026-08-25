namespace ECommerce.Application.Abstractions;

/// <summary>
/// Sends emails. Implementations may use SMTP, SendGrid, etc.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
