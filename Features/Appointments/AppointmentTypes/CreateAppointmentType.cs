using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Appointments.AppointmentTypes;

public record CreateAppointmentTypeRequest(
    string Name,
    string Description
);

public record CreateAppointmentTypeResponse(
    Guid Id,
    string Name,
    string Description
);

public class CreateAppointmentTypeEndpoint : Endpoint<CreateAppointmentTypeRequest, CreateAppointmentTypeResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateAppointmentTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/appointment-types");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentTypes"));
    }

    public override async Task HandleAsync(CreateAppointmentTypeRequest request, CancellationToken cancellationToken)
    {
        var appointmentType = new AppointmentType
        {
            Name = request.Name,
            Description = request.Description,
        };

        _context.AppointmentTypes.Add(appointmentType);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new CreateAppointmentTypeResponse(appointmentType.Id, appointmentType.Name, appointmentType.Description),
            cancellation: cancellationToken);
    }
}

public sealed class CreateAppointmentTypeValidator : Validator<CreateAppointmentTypeRequest>
{
    public CreateAppointmentTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}