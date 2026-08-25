namespace ECommerce.Application.Jobs;

/// <summary>
/// Handles a specific background job type.
/// </summary>
public interface IJobHandler<in TJob> where TJob : IJob
{
    Task HandleAsync(TJob job, CancellationToken cancellationToken = default);
}
