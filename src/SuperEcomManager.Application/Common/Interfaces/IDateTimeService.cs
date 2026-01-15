namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for date/time operations.
/// Abstracted for testability.
/// </summary>
public interface IDateTimeService
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time for the tenant's timezone.
    /// </summary>
    DateTime LocalNow { get; }

    /// <summary>
    /// Converts UTC time to the tenant's local timezone.
    /// </summary>
    DateTime ToLocal(DateTime utcDateTime);

    /// <summary>
    /// Converts local time to UTC.
    /// </summary>
    DateTime ToUtc(DateTime localDateTime);
}
