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
    /// Remove clients credit by amount.
    /// </summary>
    /// <param name="clients"></param>
    /// <param name="amount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RemoveClientsCredits(List<Client> clients, int amount, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get clients by appointment id parameter.
    /// </summary>
    /// <param name="appointmentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<Client>> GetClientsByAppointmentId(Guid appointmentId, CancellationToken cancellationToken);
}