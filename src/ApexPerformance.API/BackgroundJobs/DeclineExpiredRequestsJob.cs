using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Requests nobody answered before the appointment started
/// can no longer be approved, so they are declined:
/// appointments clients asked for (Pending) and cancelation
/// or join requests (Pending or InProgress).
/// </summary>
public class DeclineExpiredRequestsJob
{
    public const string JobId = "decline-expired-requests";

    public const string Schedule = "*/10 * * * *";

    private readonly ApexPerformanceContext _context;
    private readonly ILogger<DeclineExpiredRequestsJob> _logger;

    public DeclineExpiredRequestsJob(ApexPerformanceContext context, ILogger<DeclineExpiredRequestsJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Run()
    {
        var now = DateTimeOffset.UtcNow;

        var declinedAppointments = await DeclineExpiredAppointments(now);

        var declinedRequests = await DeclineExpiredAppointmentRequests(now);

        if (declinedAppointments + declinedRequests > 0)
            _logger.LogInformation(
                "Declined {Appointments} expired appointment requests and {Requests} expired cancelation/join requests.",
                declinedAppointments, declinedRequests);
    }

    private async Task<int> DeclineExpiredAppointments(DateTimeOffset now)
    {
        var declinedStatus = await _context.AppointmentStatuses
            .SingleAsync(x => x.Name == BusinessStatuses.Declined);

        var expiredAppointments = await _context.Appointments
            .Where(appointment =>
                appointment.AppointmentStatus.Name == BusinessStatuses.Pending &&
                appointment.StartTime <= now)
            .ToListAsync();

        foreach (var appointment in expiredAppointments)
            appointment.AppointmentStatus = declinedStatus;

        await _context.SaveChangesAsync();

        return expiredAppointments.Count;
    }

    private async Task<int> DeclineExpiredAppointmentRequests(DateTimeOffset now)
    {
        var declinedStatus = await _context.AppointmentRequestStatuses
            .SingleAsync(x => x.Name == BusinessStatuses.Declined);

        var expiredRequests = await _context.AppointmentRequests
            .Where(request =>
                (request.AppointmentRequestStatus.Name == BusinessStatuses.Pending ||
                 request.AppointmentRequestStatus.Name == BusinessStatuses.InProgress) &&
                request.Appointment.StartTime <= now)
            .ToListAsync();

        foreach (var request in expiredRequests)
            request.AppointmentRequestStatus = declinedStatus;

        await _context.SaveChangesAsync();

        return expiredRequests.Count;
    }
}
