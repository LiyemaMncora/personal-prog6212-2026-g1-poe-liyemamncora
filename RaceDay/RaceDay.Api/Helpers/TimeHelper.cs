namespace RaceDay.Api.Helpers;
public static class TimeHelper
{
    public static bool IsValidTimeOfDay(TimeSpan time)
    {
        return time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
    }

    public static bool IsValidFinishTime(TimeSpan time)
    {
        return time > TimeSpan.Zero && time < TimeSpan.FromDays(1);
    }

    public static TimeSpan? ToWholeSeconds(TimeSpan? time)
    {
        if (time == null)
        {
            return null;
        }

        return TimeSpan.FromSeconds(Math.Round(time.Value.TotalSeconds));
    }
}