using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IClientService
{
    /// <summary>
    /// Add clients new credit by amount.
    /// </summary>
    /// <param name="clients"></param>
    /// <param name="amount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddClientsCredits(List<Client> clients, int amount, CancellationToken cancellationToken);
    
    /// <summary>
    /// Remove clients' credit by amount.
    /// </summary>
    /// <param name="clients"></param>
    /// <param name="amount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RemoveClientsCredits(List<Client> clients, int amount, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get clients by appointment id parameter.
    /// </summary>
    /// <param name="appointmentId">ID of selected appointment.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task<List<Client>> GetClientsByAppointmentId(Guid appointmentId, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client">Selected client</param>
    /// <param name="coachesIds">Coaches that need to be updated.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task UpdateClientCoaches(Client client, List<Guid> coachesIds, CancellationToken cancellationToken);
}