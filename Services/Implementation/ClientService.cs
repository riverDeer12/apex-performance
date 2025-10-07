using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

public class ClientService : IClientService
{
    private readonly ApexPerformanceContext _context;
    private readonly IEmailService _emailService;
    
    public ClientService(ApexPerformanceContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task AddClientsCredits(List<Client> clients, int amount, CancellationToken cancellationToken)
    {
        foreach (var client in clients)
        {
            client.Credits += amount;
        }

        _context.Clients.UpdateRange(clients);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);
    }

    public async Task RemoveClientsCredits(List<Client> clients, int amount, CancellationToken cancellationToken)
    {
        foreach (var client in clients)
        {
            client.Credits -= amount;
            
            if(client.Credits == 1)
                _emailService.SendLowCreditsAlertEmail(client);
        }

        _context.Clients.UpdateRange(clients);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);
    }

    public async Task<List<Client>> GetClientsByAppointmentId(Guid appointmentId, CancellationToken cancellationToken)
    {
        var clientsIds = await _context.ClientAppointments
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => x.AppointmentId)
            .ToListAsync(cancellationToken: cancellationToken);

        return await _context.Clients.Where(x => clientsIds
            .Contains(x.Id)).ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task UpdateClientCoaches(Client client, List<Guid> coachesIds, CancellationToken cancellationToken)
    {
        if (coachesIds.Count == 0) return;
        
        await _context.CoachClients
            .Where(coachClient => coachClient.ClientId == client.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var coaches = await _context.Coaches
            .Where(x => coachesIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var clientCoaches = coaches
            .Select(coach => new CoachClient
            {
                Client = client,
                ClientId = client.Id,
                CoachId = coach.Id,
                Coach = coach
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(clientCoaches, cancellationToken: cancellationToken);
    }
}