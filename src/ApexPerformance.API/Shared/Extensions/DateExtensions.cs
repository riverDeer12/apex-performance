namespace ApexPerformance.API.Shared.Extensions;

public static class DateExtensions
{
    public static DateTimeOffset CombineDateAndTime(DateTimeOffset date, TimeOnly time)
    {
        var utcDateTime = new DateTime(
            date.Year, date.Month, date.Day,
            time.Hour, time.Minute, time.Second,
            DateTimeKind.Utc
        );

        return new DateTimeOffset(utcDateTime);
    }

    public static DateTimeOffset GetNextWeekday(DayOfWeek day)
    {
        DateTime today = DateTime.Today;

        int daysToAdd = ((int)day - (int)today.DayOfWeek + 7) % 7;

        if (daysToAdd == 0) daysToAdd = 7;

        return new DateTimeOffset(today.AddDays(daysToAdd), DateTimeOffset.Now.Offset);
    }
    
    /// <summary>
    /// Returns the next date of the specified day of week after the given start date.
    /// </summary>
    /// <param name="day">The day of week to find (e.g., DayOfWeek.Monday).</param>
    /// <param name="startDate">The date to start searching from. Defaults to today.</param>
    /// <returns>The next DateTime that falls on the specified day of the week.</returns>
    public static DateTime GetNextDateOfDay(DayOfWeek day, DateTime? startDate = null)
    {
        DateTime fromDate = startDate ?? DateTime.Today;
        
        // Calculate the days to add to reach the next desired day
        int daysToAdd = ((int)day - (int)fromDate.DayOfWeek + 7) % 7;

        // If today is the requested day, move to the next week's occurrence
        if (daysToAdd == 0)
            daysToAdd = 7;

        return fromDate.AddDays(daysToAdd);
    }
}