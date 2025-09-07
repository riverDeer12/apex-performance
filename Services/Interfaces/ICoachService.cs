using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface ICoachService
{
    /// <summary>
    /// Helper for updating appointment clients.
    /// </summary>
    /// <param name="coach">Selected coach.</param>
    /// <param name="clientsIds">List of  clients ids.</param>
    /// <param name="cancellationToken">Value of a cancellation token.</param>
    /// <returns></returns>
    Task UpdateCoachClients(Coach coach, List<Guid> clientsIds, CancellationToken cancellationToken);
}