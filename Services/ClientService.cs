using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services;

public class ClientService : IClientService
{
    private readonly ApexPerformanceContext _context;

    public ClientService(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public async Task AddClientsCredits(List<Client> clients, int amount, CancellationToken cancellationToken)
    {
        foreach (var client in clients)
        {
            client.Credits = +amount;
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
            client.Credits = -amount;
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
}