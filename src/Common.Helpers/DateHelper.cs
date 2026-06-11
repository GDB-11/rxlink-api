namespace Common.Helpers;

public static class DateHelper
{
    public static DateOnly ToDateOnly(this DateTime date) => new(date.Year, date.Month, date.Day);

    public static DateOnly? ToDateOnly(this DateTime? date)
    {
        if (date is null) return null;

        return new DateOnly(date.Value.Year, date.Value.Month, date.Value.Day);
    }

    public static DateTime ToDateTime(this DateOnly date) => new(date.Year, date.Month, date.Day);

    public static DateTime? ToDateTime(this DateOnly? date)
    {
        if (date is null) return null;

        return new DateTime(date.Value.Year, date.Value.Month, date.Value.Day);
    }
}