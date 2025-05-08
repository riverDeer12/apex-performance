namespace ApexPerformance.API.Services;

public interface IAppointmentService
{
    /// <summary>
    /// Check if there is available time slot for new appointment.
    /// </summary>
    /// <param name="appointmentStartTime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> CheckFreeTimeSlot(DateTimeOffset appointmentStartTime, CancellationToken cancellationToken);
}