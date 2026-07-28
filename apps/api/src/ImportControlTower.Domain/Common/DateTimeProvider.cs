namespace ImportControlTower.Domain.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo IstanbulTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time") ?? 
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTime ToIstanbulTime(this DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc ? utcDateTime : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, IstanbulTimeZone);
    }
}
