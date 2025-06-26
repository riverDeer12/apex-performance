using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentStatuses;

public record UpdateAppointmentStatusRequest(
    string Name,
    string Description
);

public record UpdateAppointmentStatusResponse(
    Guid Id
);

public class UpdateAppointmentStatusEndpoint : Endpoint<UpdateAppointmentStatusRequest, UpdateAppointmentStatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateAppointmentStatusEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/appointment-statuses/{id}");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentStatuses"));
    }

    public override async Task HandleAsync(UpdateAppointmentStatusRequest request, CancellationToken cancellationToken)
    {
        var appointmentStatusId = Route<Guid>("id", isRequired: true);

        var appointmentStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Id == appointmentStatusId, cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointmentStatus.Name = request.Name;
        appointmentStatus.Description = request.Description;

        _context.AppointmentStatuses.Update(appointmentStatus);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new UpdateAppointmentStatusResponse(appointmentStatus.Id),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateAppointmentStatusValidator : Validator<UpdateAppointmentStatusRequest>
{
    public UpdateAppointmentStatusValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}