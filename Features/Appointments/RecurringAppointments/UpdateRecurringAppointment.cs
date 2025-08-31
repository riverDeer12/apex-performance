using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record UpdateRecurringAppointmentRequest(List<Guid> Clients, Guid Coach, Guid Type);

public record UpdateRecurringAppointmentResponse(Guid Id);

public class
    UpdateRecurringAppointmentEndpoint : Endpoint<UpdateRecurringAppointmentRequest, UpdateRecurringAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IRecurringAppointmentService _recurringAppointmentService;

    public UpdateRecurringAppointmentEndpoint(ApexPerformanceContext context,
        IRecurringAppointmentService recurringAppointmentService)
    {
        _context = context;
        _recurringAppointmentService = recurringAppointmentService;
    }

    public override void Configure()
    {
        Put("api/recurring-appointments/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(UpdateRecurringAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var recurringAppointmentId = Route<Guid>("id", isRequired: true);

        var recurringAppointment =
            await _context.RecurringAppointments
                .FirstOrDefaultAsync(x => x.Id == recurringAppointmentId,
                    cancellationToken: cancellationToken);

        if (recurringAppointment is null)
            ThrowError(ErrorMessages.NotFound);

        var coach = await _context.Coaches.SingleAsync(x => x.Id == request.Coach,
            cancellationToken: cancellationToken);

        var type = await _context.AppointmentTypes.SingleAsync(x => x.Id == request.Type,
            cancellationToken: cancellationToken);

        var clients = await _context.Clients.Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        recurringAppointment.Coach = coach;
        recurringAppointment.AppointmentType = type;

        _context.RecurringAppointments.Update(recurringAppointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await _recurringAppointmentService.UpdateClients(clients, recurringAppointment, cancellationToken);

        await SendAsync(new UpdateRecurringAppointmentResponse(recurringAppointment.Id),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateRecurringAppointmentValidator
    : Validator<UpdateRecurringAppointmentRequest>
{
    public UpdateRecurringAppointmentValidator()
    {
        RuleFor(x => x.Clients)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ValidationMessages.DuplicatesNotAllowed)
            .MustAsync(async (clientIds, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();
                var numberOfClients = await db.Clients
                    .Where(client => clientIds.Contains(client.Id))
                    .CountAsync(cancellationToken);

                return numberOfClients == clientIds.Count;
            })
            .WithMessage(ErrorMessages.NotFound);

        RuleFor(x => x.Coach)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                return db.Coaches.AnyAsync(coach => coach.Id == id, cancellationToken);
            })
            .WithMessage(ErrorMessages.NotFound);

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                return db.AppointmentTypes.AnyAsync(appointmentType => appointmentType.Id == id,
                    cancellationToken);
            })
            .WithMessage(ErrorMessages.NotFound);
    }
}