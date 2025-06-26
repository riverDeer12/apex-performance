using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IAppointmentService
{
    /// <summary>
    /// Check if there is available time for a new appointment.
    /// It is allowed to take an appointment that starts at
    /// the time when the previous one is finished.
    /// </summary>
    /// <param name="appointmentStartTime">Start of new appointment.</param>
    /// <param name="appointmentEndTime">End of new appointment.</param>
    /// <param name="cancellationToken">Cancellation Token value.</param>
    /// <param name="appointmentId">Id of appointment.</param>
    /// <returns></returns>
    Task<bool> CheckFreeSlot(DateTimeOffset appointmentStartTime, DateTimeOffset appointmentEndTime,
        CancellationToken cancellationToken, Guid? appointmentId = null);

    /// <summary>
    /// Helper for updating appointment clients.
    /// </summary>
    /// <param name="clients">List of selected clients.</param>
    /// <param name="appointment">Selected appointment.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task UpdateClients(List<Client> clients, Appointment appointment, CancellationToken cancellationToken);

    /// <summary>
    /// Helper for updating appointment coaches.
    /// </summary>
    /// <param name="coaches">List of selected coaches.</param>
    /// <param name="appointment">Selected appointment.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task UpdateCoaches(List<Coach> coaches, Appointment appointment, CancellationToken cancellationToken);
}