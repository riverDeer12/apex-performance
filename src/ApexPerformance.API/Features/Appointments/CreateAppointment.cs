using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

[UsedImplicitly]
public record CreateAppointmentRequest(
    Guid Type,
    Guid TimeSlot,
    Guid Location,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients,
    List<Guid> Coaches
);

public record AppointmentClientDto(
    Guid Id,
    string FirstName,
    string LastName
);

public class CreateAppointmentEndpoint : Endpoint<CreateAppointmentRequest, StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;
    private readonly IEmailService _emailService;
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;

    public CreateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService,
        IEmailService emailService, ICurrentUserService currentUserService, IClientService clientService)
    {
        _context = context;
        _appointmentService = appointmentService;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _clientService = clientService;
    }

    public override void Configure()
    {
        Post("api/appointments");
        Permissions(UserPermissions.CanCreateAppointment);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var clients = await _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (!_appointmentService.CheckClientsCredits(clients, cancellationToken))
            ThrowError("Client does not have any credits available.");

        var coaches = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        var appointmentType = await _context.AppointmentTypes
            .SingleAsync(x => x.Id == request.Type,
                cancellationToken: cancellationToken);

        var timeSlot =
            await _context.TimeSlots
                .SingleAsync(x => x.Id == request.TimeSlot,
                    cancellationToken: cancellationToken);

        if (!await CheckValidity(request.StartTime, timeSlot, cancellationToken))
            ThrowError("Appointment is not valid.");

        var appointment = new Appointment
        {
            AppointmentType = appointmentType,
            AppointmentStatus = await GetAppointmentStatus(cancellationToken),
            TimeSlot = timeSlot,
            StartTime = request.StartTime.UtcDateTime,
            EndTime = request.EndTime.UtcDateTime
        };

        _context.Appointments.Add(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _appointmentService.UpdateClients(clients, appointment, cancellationToken);

        await _appointmentService.UpdateCoaches(coaches, appointment, cancellationToken);

        if (appointment.AppointmentStatus.Name == BusinessStatuses.Approved)
            await _clientService.RemoveClientsCredits(clients, 1, cancellationToken);
        
        BackgroundJob.Enqueue(() =>
            SendNotificationEmails(request.Coaches, request.Clients, appointment.Id, timeSlot.Id, cancellationToken));
        
        await SendAsync(
            new StatusResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
    
    public async Task SendNotificationEmails(List<Guid> coachesIds, List<Guid> clientsIds, Guid appointmentId,
        Guid timeSlotId, CancellationToken cancellationToken)
    {
        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.AppointmentStatus)
                .SingleAsync(x => x.Id == appointmentId,
                    cancellationToken: cancellationToken);
        
        var clients = await _context.Clients
            .Where(x => clientsIds.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);
        
        var coaches = await _context.Coaches
            .Where(x => coachesIds.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);
        
        var timeSlot =
            await _context.TimeSlots
                .SingleAsync(x => x.Id == timeSlotId,
                    cancellationToken: cancellationToken);
        
        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
        {
            _emailService.SendAppointmentRequestEmail(coaches, clients, appointment, timeSlot);
        }
        else
        {
            _emailService.SendAppointmentStatus(clients, appointment, timeSlot);
        }
    }

    private async Task<bool> CheckValidity(DateTimeOffset requestStartTime, TimeSlot timeSlot,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;

        var requestedDay = requestStartTime.Date;

        if (today == requestedDay && _currentUserService.LoggedUserHasRole(UserRoles.Client))
            return false;
        
        if (!await _appointmentService.CheckFreeSlot(requestStartTime, timeSlot, cancellationToken))
            return false;

        return true;
    }

    private async Task<AppointmentStatus> GetAppointmentStatus(CancellationToken cancellationToken)
    {
        var pendingStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Pending),
                    cancellationToken: cancellationToken);

        if (pendingStatus is null)
            ThrowError("Pending Status not found.");

        var approvedStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Approved),
                    cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError("Approved Status not found.");

        return _currentUserService.LoggedUserHasRole(UserRoles.Client) ? pendingStatus : approvedStatus;
    }
}

public sealed class CreateAppointmentValidator : Validator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                
                return db.AppointmentTypes.AnyAsync(appointmentType => appointmentType.Id == id, cancellationToken);
            })
            .WithMessage(ErrorCodes.NotFound);
        
        RuleFor(x => x.TimeSlot)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                
                return db.TimeSlots.AnyAsync(appointmentType => appointmentType.Id == id, cancellationToken);
            })
            .WithMessage(ErrorCodes.NotFound);
        
        RuleFor(x => x.Clients)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ErrorCodes.DuplicatesNotAllowed)
            .MustAsync(async (clientIds, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();
                var numberOfClients = await db.Clients
                    .Where(client => clientIds.Contains(client.Id))
                    .CountAsync(cancellationToken);

                return numberOfClients == clientIds.Count;
            })
            .WithMessage(ErrorCodes.NotFound);
        
        RuleFor(x => x.Coaches)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ErrorCodes.DuplicatesNotAllowed)
            .MustAsync(async (coachIds, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();
                var numberOfCoaches = await db.Coaches
                    .Where(coach => coachIds.Contains(coach.Id))
                    .CountAsync(cancellationToken);

                return numberOfCoaches == coachIds.Count;
            })
            .WithMessage(ErrorCodes.NotFound);
    }
}