using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using EFCore.BulkExtensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record UpdateAppointmentRequest(
    Guid Type,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients
);

public record UpdateAppointmentResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<AppointmentClientDto> Clients
);

public class UpdateAppointmentEndpoint : Endpoint<UpdateAppointmentRequest, UpdateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;
    private readonly IClientService _clientService;

    public UpdateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService,
        IClientService clientService)
    {
        _context = context;
        _appointmentService = appointmentService;
        _clientService = clientService;
    }

    public override void Configure()
    {
        Put("api/appointments/{id}");
        Permissions(nameof(UserPermissions.CanUpdateAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(UpdateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        if (!await _appointmentService.CheckFreeSlot(request.StartTime, request.EndTime,
                cancellationToken, appointmentId))
            ThrowError(ValidationMessages.NotValid);

        var appointmentType = await _context.AppointmentTypes.FirstOrDefaultAsync(
            x => x.Id == request.Type, cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        appointment.AppointmentType = appointmentType;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var appointmentClients = new List<ClientAppointment>(
            request.Clients.Select(clientId => new ClientAppointment
            {
                ClientId = clientId,
                AppointmentId = appointment.Id
            }));

        await _context.ClientAppointments
            .Where(clientAppointment => clientAppointment.AppointmentId == appointment.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.BulkInsertOrUpdateAsync(appointmentClients, cancellationToken: cancellationToken);

        var clients = await _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        var clientsResponse = clients
            .Select(client => new AppointmentClientDto(client.Id, client.FirstName, client.LastName))
            .ToList();

        await _clientService.RemoveClientsCredits(clients, 1, cancellationToken);

        await SendAsync(
            new UpdateAppointmentResponse(appointment.Id, appointment.StartTime, appointment.EndTime, clientsResponse),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateAppointmentValidator : Validator<UpdateAppointmentRequest>
{
    public UpdateAppointmentValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.StartTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.EndTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}