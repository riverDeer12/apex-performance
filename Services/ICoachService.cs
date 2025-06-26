using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface ICoachService
{
    /// <summary>
    /// Helper for updating appointment clients.
    /// </summary>
    /// <param name="clients">List of selected clients.</param>
    /// <param name="coach">Selected coach.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task UpdateCoachClients(List<Client> clients, Coach coach, CancellationToken cancellationToken);
}