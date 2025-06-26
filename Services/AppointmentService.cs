using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;

namespace ApexPerformance.API.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApexPerformanceContext _context;

    public AppointmentService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public Task<bool> CheckFreeSlot(DateTimeOffset appointmentStartTime,
        DateTimeOffset appointmentEndTime, CancellationToken cancellationToken, Guid? appointmentId = null)
    {
        return Task.FromResult(!_context.Appointments.Any(existing =>
            appointmentStartTime < existing.EndTime && appointmentEndTime > existing.StartTime &&
            existing.Id != appointmentId
        ));
    }

    public async Task UpdateCoaches(List<Coach> coaches, Appointment appointment,
        CancellationToken cancellationToken)
    {
        var appointmentCoaches = coaches
            .Select(coach => new CoachAppointment
            {
                CoachId = coach.Id,
                Coach = coach,
                AppointmentId = appointment.Id,
                Appointment = appointment
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(appointmentCoaches, cancellationToken: cancellationToken);
    }

    public async Task UpdateClients(List<Client> clients, Appointment appointment,
        CancellationToken cancellationToken)
    {
        var appointmentClients = clients
            .Select(client => new ClientAppointment
            {
                ClientId = client.Id,
                Client = client,
                AppointmentId = appointment.Id,
                Appointment = appointment
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(appointmentClients, cancellationToken: cancellationToken);
    }
}