using ApexPerformance.API.Constants;

namespace ApexPerformance.API.Utilities;

/// <summary>
/// The client's package on the apps' home screen ("4 od 12 treninga
/// odrađeno"): trainings done this month are the client's approved
/// appointments (or in progress) of this month that are over. Credits are
/// taken when an appointment is approved, so the apps add the upcoming
/// approved appointments and the credits left to get the total.
/// GET api/appointments gives a client only upcoming appointments, so the
/// apps can't count the done ones themselves: current-client sends
/// DoneThisMonth. The month is Croatian time, like the gym's schedule.
/// </summary>
public static class ClientPackage
{
    public static readonly string[] DoneStatuses = [BusinessStatuses.Approved, BusinessStatuses.InProgress];

    /// <summary>Start of the month <paramref name="now"/> is in, Croatian time.</summary>
    public static DateTimeOffset MonthStart(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(now, timeZone);
        var monthStart = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(monthStart, timeZone.GetUtcOffset(monthStart));
    }

    public static bool IsDoneThisMonth(DateTimeOffset startTime, DateTimeOffset endTime, string status,
        bool isDeleted, DateTimeOffset now, TimeZoneInfo timeZone) =>
        !isDeleted &&
        DoneStatuses.Contains(status) &&
        endTime <= now &&
        startTime >= MonthStart(now, timeZone);
}
