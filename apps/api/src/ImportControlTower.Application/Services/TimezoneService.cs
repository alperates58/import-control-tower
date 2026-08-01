using System;

namespace ImportControlTower.Application.Services;

public interface ITimezoneService
{
    bool IsValidTimezoneId(string timezoneId);
    DateTime ConvertLocalToUtc(DateTime localDateTime, string timezoneId, out string? errorCode);
}

public class TimezoneService : ITimezoneService
{
    public bool IsValidTimezoneId(string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId)) return false;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DateTime ConvertLocalToUtc(DateTime localDateTime, string timezoneId, out string? errorCode)
    {
        errorCode = null;
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            errorCode = "TIMEZONE_REQUIRED";
            return localDateTime;
        }

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch
        {
            errorCode = "INVALID_TIMEZONE_ID";
            return localDateTime;
        }

        // Check DST invalid or ambiguous local time
        if (tz.IsInvalidTime(localDateTime))
        {
            errorCode = "INVALID_LOCAL_DATETIME";
            return localDateTime;
        }

        if (tz.IsAmbiguousTime(localDateTime))
        {
            errorCode = "AMBIGUOUS_LOCAL_DATETIME";
            return localDateTime;
        }

        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }

        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }
}
