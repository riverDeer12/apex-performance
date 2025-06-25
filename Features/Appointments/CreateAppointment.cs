using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using EFCore.BulkExtensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record CreateAppointmentRequest(
    Guid Type,
    Guid Status,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients
);

public record CreateAppointmentResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<AppointmentClientDto> Clients
);

public record AppointmentClientDto(
    Guid Id,
    string FirstName,
    string LastName
);

public class CreateAppointmentEndpoint : Endpoint<CreateAppointmentRequest, CreateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;
    private readonly IClientService _clientService;
    private readonly IEmailService _emailService;

    public CreateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService,
        IClientService clientService, IEmailService emailService)
    {
        _context = context;
        _appointmentService = appointmentService;
        _clientService = clientService;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Post("api/appointments");
        Permissions(nameof(UserPermissions.CanCreateAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var clients = await _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count == 0)
            ThrowError(ErrorMessages.NotFound);

        var appointmentType = await _context.AppointmentTypes
            .FirstOrDefaultAsync(x => x.Id == request.Type,
                cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        if (!await _appointmentService.CheckFreeSlot(request.StartTime, request.EndTime, cancellationToken))
            ThrowError(ValidationMessages.NotValid);

        var pendingStatus =
            await _context.AppointmentStatuses.FirstOrDefaultAsync(x => x.Name == BusinessStatuses.Pending.Name,
                cancellationToken: cancellationToken);

        if (pendingStatus is null)
            ThrowError(ErrorMessages.NotFound);

        var appointment = new Appointment
        {
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            AppointmentType = appointmentType,
            AppointmentStatus = pendingStatus
        };

        _context.Appointments.Add(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var appointmentClients = clients
            .Select(client => new ClientAppointment
            {
                ClientId = client.Id,
                Client = client,
                AppointmentId = appointment.Id,
                Appointment = appointment
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(appointmentClients, cancellationToken: cancellationToken);

        var clientsResponse = clients
            .Select(client => new AppointmentClientDto(client.Id, client.FirstName, client.LastName))
            .ToList();
        
        _emailService.SendAppointmentEmailToClients(clients, appointment);

        await SendAsync(
            new CreateAppointmentResponse(appointment.Id, appointment.StartTime, appointment.EndTime,
                clientsResponse),
            cancellation: cancellationToken);
    }
}

public sealed class CreateAppointmentValidator : Validator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.StartTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.EndTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}