namespace PurchaseApproval.Utils;

public static class DateTimeHelper
{
    private static readonly TimeZoneInfo BeijingTimeZone = ResolveBeijingTimeZone();

    public static DateTime GetBeijingTime()
    {
        return TimeZoneInfo.ConvertTime(DateTime.UtcNow, BeijingTimeZone).ReplaceKind(DateTimeKind.Unspecified);
    }

    public static DateTime UtcToBeijing(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTime(utc, BeijingTimeZone).ReplaceKind(DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo ResolveBeijingTimeZone()
    {
        var candidates = new[] { "Asia/Shanghai", "China Standard Time" };
        foreach (var candidate in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException("无法解析北京时间时区，请检查系统时区配置");
    }

    private static DateTime ReplaceKind(this DateTime dateTime, DateTimeKind kind)
    {
        return DateTime.SpecifyKind(dateTime, kind);
    }
}
