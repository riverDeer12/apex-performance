using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record CreateAppointmentRequest(
    Guid AppointmentType,
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
    string Firstname,
    string Lastname
);

public class CreateAppointmentEndpoint : Endpoint<CreateAppointmentRequest, CreateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;
    private readonly IClientService _clientService;

    public CreateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService,
        IClientService clientService)
    {
        _context = context;
        _appointmentService = appointmentService;
        _clientService = clientService;
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
            .FirstOrDefaultAsync(x => x.Id == request.AppointmentType,
                cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        if (!await _appointmentService.CheckFreeSlot(request.StartTime, cancellationToken))
            ThrowError(ValidationMessages.NotValid);

        var appointment = new Appointment
        {
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            AppointmentType = appointmentType
        };

        _context.Appointments.Add(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var clientAppointments = clients
            .Select(client => new ClientAppointment
            {
                ClientId = client.Id,
                Client = client,
                AppointmentId = appointment.Id,
                Appointment = appointment
            }).ToList();

        _context.ClientAppointments.AddRange(clientAppointments);

        var relationsResult = await _context.SaveChangesAsync(cancellationToken);

        if (relationsResult == 0)
            ThrowError(ErrorMessages.SavingError);

        var clientsResponse = clients
            .Select(client => new AppointmentClientDto(client.Id, client.FirstName, client.LastName))
            .ToList();

        await _clientService.RemoveClientsCredits(clients, 1, cancellationToken);

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
        RuleFor(x => x.AppointmentType).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.StartTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.EndTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}