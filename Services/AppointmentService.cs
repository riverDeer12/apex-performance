using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

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

    public async void ChangeAppointmentStatus(Appointment appointment, AppointmentRequestStatus requestStatus,
        string businessAction, CancellationToken cancellationToken)
    {
        var updatedStatus = businessAction switch
        {
            BusinessActions.CancelationRequest => await _context.AppointmentStatuses.FirstOrDefaultAsync(
                x => x.Name == nameof(BusinessStatuses.Canceled), cancellationToken: cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(businessAction), businessAction, null)
        };
        
        appointment.AppointmentStatus = updatedStatus!;

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