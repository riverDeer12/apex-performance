namespace ApexPerformance.API.Services;

public interface IAppointmentService
{
    /// <summary>
    /// Check if there is available time for new appointment.
    /// </summary>
    /// <param name="appointmentStartTime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> CheckFreeSlot(DateTimeOffset appointmentStartTime, CancellationToken cancellationToken);
}