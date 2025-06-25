using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IAppointmentService
{
    /// <summary>
    /// Check if there is available time for new appointment.
    /// It is allowed to take appointment that starts at
    /// the time when previous one is finished.
    /// </summary>
    /// <param name="appointmentStartTime">Start of new appointment.</param>
    /// <param name="appointmentEndTime">End of new appointment.</param>
    /// <param name="cancellationToken">Cancellation Token value.</param>
    /// <param name="appointmentId">Id of appointment.</param>
    /// <returns></returns>
    Task<bool> CheckFreeSlot(DateTimeOffset appointmentStartTime, DateTimeOffset appointmentEndTime,
        CancellationToken cancellationToken, Guid? appointmentId = null);
}