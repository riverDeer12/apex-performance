using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

public class CoachService : ICoachService
{
    private readonly ApexPerformanceContext _context;

    public CoachService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public async Task UpdateCoachClients(Coach coach, List<Guid> clientsIds, CancellationToken cancellationToken)
    {
        if (clientsIds.Count == 0) return;

        await _context.CoachClients
            .Where(coachClient => coachClient.CoachId == coach.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var clients = await _context.Clients
            .Where(x => clientsIds.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count == 0)
            return;

        await _context.CoachClients
            .Where(coachClient => coachClient.CoachId == coach.Id)
            .ExecuteDeleteAsync(cancellationToken);

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