using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of IDateTimeService.
/// </summary>
public class DateTimeService : IDateTimeService
{
    // Default timezone for India
    private static readonly TimeZoneInfo _defaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LocalNow => ToLocal(UtcNow);

    public DateTime ToLocal(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _defaultTimeZone);
    }

    public DateTime ToUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, _defaultTimeZone);
    }
}
