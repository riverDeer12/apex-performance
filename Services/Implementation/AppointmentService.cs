using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

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

    public async void ChangeAppointmentStatus(Appointment appointment,
        string businessAction, CancellationToken cancellationToken)
    {
        if (businessAction != BusinessActions.CancelationRequest) return;
        
        var updatedStatus = await _context.AppointmentStatuses.SingleAsync(
            x => x.Name == BusinessStatuses.Canceled, cancellationToken: cancellationToken);
            
        appointment.AppointmentStatus = updatedStatus;
            
        _context.Appointments.Update(appointment);
        
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception("Error changing Appointment status.");
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