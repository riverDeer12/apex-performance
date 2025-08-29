using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;

namespace ApexPerformance.API.Services;

public class RecurringAppointmentService : IRecurringAppointmentService
{
    private readonly ApexPerformanceContext _context;

    public RecurringAppointmentService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public async Task UpdateClients(List<Client> clients, RecurringAppointment recurring,
        CancellationToken cancellationToken)
    {
        var appointmentClients = clients
            .Select(client => new ClientRecurringAppointment()
            {
                ClientId = client.Id,
                Client = client,
                RecurringAppointmentId = recurring.Id,
                RecurringAppointment = recurring
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(appointmentClients, cancellationToken: cancellationToken);
    }
}