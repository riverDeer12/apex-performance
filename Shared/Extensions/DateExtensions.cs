namespace ApexPerformance.API.Shared.Extensions;

public class DateExtensions
{
    public static DateTimeOffset CombineDateAndTime(DateTimeOffset date, TimeOnly time)
    {
        return new DateTimeOffset(
            date.Year, date.Month, date.Day,
            time.Hour, time.Minute,
            time.Second,
            date.Offset
        );
    }

    public static DateTimeOffset GetNextWeekday(DayOfWeek day)
    {
        DateTime today = DateTime.Today;

        int daysToAdd = ((int)day - (int)today.DayOfWeek + 7) % 7;

        if (daysToAdd == 0) daysToAdd = 7;

        return new DateTimeOffset(today.AddDays(daysToAdd), DateTimeOffset.Now.Offset);
    }
}