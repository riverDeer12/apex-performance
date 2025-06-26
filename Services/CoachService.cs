using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;

namespace ApexPerformance.API.Services;

public class CoachService : ICoachService
{
    private readonly ApexPerformanceContext _context;

    public CoachService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public async Task UpdateCoachClients(List<Client> clients, Coach coach, CancellationToken cancellationToken)
    {
        var appointmentClients = clients
            .Select(client => new CoachClient
            {
                ClientId = client.Id,
                Client = client,
                CoachId = coach.Id,
                Coach = coach
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(appointmentClients, cancellationToken: cancellationToken);
    }
}