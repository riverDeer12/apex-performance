using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentTypes;

public record UpdateAppointmentTypeRequest(
    string Name,
    string Description
);

public record UpdateAppointmentTypeResponse(
    Guid Id
);

public class UpdateAppointmentTypeEndpoint : Endpoint<UpdateAppointmentTypeRequest, UpdateAppointmentTypeResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateAppointmentTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/appointment-types/{id}");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentTypes"));
    }

    public override async Task HandleAsync(UpdateAppointmentTypeRequest request, CancellationToken cancellationToken)
    {
        var appointmentTypeId = Route<Guid>("id", isRequired: true);

        var appointmentType =
            await _context.AppointmentTypes
                .FirstOrDefaultAsync(x => x.Id == appointmentTypeId, cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        appointmentType.Name = request.Name;
        appointmentType.Description = request.Description;

        _context.AppointmentTypes.Update(appointmentType);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new UpdateAppointmentTypeResponse(appointmentType.Id),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateAppointmentTypeValidator : Validator<UpdateAppointmentTypeRequest>
{
    public UpdateAppointmentTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}