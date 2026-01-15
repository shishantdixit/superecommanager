namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Interface for scheduling and managing background jobs.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a job to be executed immediately.
    /// </summary>
    Task EnqueueAsync<TJob>(object? args = null, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Schedules a job to run at a specific time.
    /// </summary>
    Task ScheduleAsync<TJob>(DateTime runAt, object? args = null, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob;

    /// <summary>
    /// Schedules a recurring job.
    /// </summary>
    Task ScheduleRecurringAsync<TJob>(string jobId, string cronExpression, object? args = null)
        where TJob : IBackgroundJob;
}

/// <summary>
/// Base interface for background jobs.
/// </summary>
public interface IBackgroundJob
{
    Task ExecuteAsync(object? args, CancellationToken cancellationToken = default);
}

/// <summary>
/// Arguments for channel sync jobs.
/// </summary>
public class ChannelSyncJobArgs
{
    public Guid? ChannelId { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
