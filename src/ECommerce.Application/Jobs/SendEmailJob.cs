namespace ECommerce.Application.Jobs;

/// <summary>
/// Sends a generic email.
/// </summary>
public record SendEmailJob(string To, string Subject, string Body) : IJob;
