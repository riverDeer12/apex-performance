using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record ApproveAppointmentResponse(
    Guid Id,
    bool IsApproved
);

public class ApproveAppointmentEndpoint : EndpointWithoutRequest<ApproveAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IEmailService _emailService;
    private readonly IClientService _clientService;
    private readonly IAppointmentService _appointmentService;

    public ApproveAppointmentEndpoint(ApexPerformanceContext context, IEmailService emailService,
        IClientService clientService, IAppointmentService appointmentService)
    {
        _context = context;
        _emailService = emailService;
        _clientService = clientService;
        _appointmentService = appointmentService;
    }

    public override void Configure()
    {
        Get("api/appointments/approve/{id}");
        Permissions(UserPermissions.CanApproveAppointment);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.Clients)
                .ThenInclude(clientAppointment => clientAppointment.Client)
                .Include(appointment => appointment.AppointmentType).Include(appointment => appointment.TimeSlot)
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.AppointmentStatus = await SetNewAppointmentStatus(appointment, cancellationToken);

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var clients = appointment.Clients.Select(x => x.Client).ToList();

        _emailService.SendAppointmentStatus(clients, appointment, appointment.TimeSlot);

        await _clientService.RemoveClientsCredits(clients, 1, cancellationToken);

        await SendAsync(
            new ApproveAppointmentResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }

    private async Task<AppointmentStatus> SetNewAppointmentStatus(Appointment appointment,
        CancellationToken cancellationToken)
    {
        var approvedStatus = await _context.AppointmentStatuses
            .FirstOrDefaultAsync(x => x.Name == BusinessStatuses.Approved, 
                cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError(ErrorMessages.NotFound);

        var declinedStatus = await _context.AppointmentStatuses
            .FirstOrDefaultAsync(x => x.Name == BusinessStatuses.Declined, 
                cancellationToken: cancellationToken);

        if (declinedStatus is null)
            ThrowError(ErrorMessages.NotFound);

        var freeTimeSlot = await _appointmentService.CheckFreeSlot(appointment.StartTime,
            appointment.EndTime, cancellationToken);

        return appointment.AppointmentStatus = freeTimeSlot ? approvedStatus : declinedStatus;
    }
}