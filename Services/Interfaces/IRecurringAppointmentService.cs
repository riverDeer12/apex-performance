using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IRecurringAppointmentService
{
    /// <summary>
    /// Helper for updating appointment clients.
    /// </summary>
    /// <param name="clients">List of selected clients.</param>
    /// <param name="recurring">Selected recurring.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task UpdateClients(List<Client> clients, RecurringAppointment recurring, CancellationToken cancellationToken);

    /// <summary>
    /// Check if there is already registered
    /// recurring appointment with values.
    /// </summary>
    /// <param name="coachId">Coach identifier.</param>
    /// <param name="timeSlotId">Timeslot identifier.</param>
    /// <returns></returns>
    bool CheckIfRecurringAvailable(Guid coachId, Guid timeSlotId);
}