using ApexPerformance.API.Constants;
using ApexPerformance.API.Utilities;

namespace ApexPerformance.Tests;

public class ClientPackageTests
{
    private static readonly TimeZoneInfo Zagreb = AppointmentReminder.CroatianTimeZone();

    // 17.11.2026. 12:00 in Zagreb (winter, UTC+1).
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 11, 0, 0, TimeSpan.Zero);

    private static bool Done(DateTimeOffset start, string status = BusinessStatuses.Approved,
        bool isDeleted = false) =>
        ClientPackage.IsDoneThisMonth(start, start.AddHours(1), status, isDeleted, Now, Zagreb);

    [Fact]
    public void Month_starts_at_midnight_croatian_time()
    {
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(1)),
            ClientPackage.MonthStart(Now, Zagreb));

        // 31.7. 23:30 UTC is already August in Zagreb (summer, UTC+2).
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(2)),
            ClientPackage.MonthStart(new DateTimeOffset(2026, 7, 31, 23, 30, 0, TimeSpan.Zero), Zagreb));
    }

    [Fact]
    public void Approved_appointments_of_this_month_that_are_over_are_done()
    {
        Assert.True(Done(Now.AddDays(-3)));
        Assert.True(Done(Now.AddHours(-1)));
        // 1.11. 00:30 Zagreb is still 31.10. in UTC.
        Assert.True(Done(new DateTimeOffset(2026, 10, 31, 23, 30, 0, TimeSpan.Zero)));
        // Started by the coach.
        Assert.True(Done(Now.AddDays(-1), BusinessStatuses.InProgress));
    }

    [Fact]
    public void Upcoming_running_last_month_and_not_approved_are_not_done()
    {
        // Booked: still on the package as reserved.
        Assert.False(Done(Now.AddDays(1)));
        // Not over yet.
        Assert.False(Done(Now.AddMinutes(-30)));
        // 31.10. 23:30 Zagreb.
        Assert.False(Done(new DateTimeOffset(2026, 10, 31, 22, 30, 0, TimeSpan.Zero)));

        foreach (var status in new[]
                 {
                     BusinessStatuses.Pending, BusinessStatuses.Declined, BusinessStatuses.Canceled
                 })
            Assert.False(Done(Now.AddDays(-2), status));

        Assert.False(Done(Now.AddDays(-2), isDeleted: true));
    }
}
