using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Appointments.AppointmentStatuses;

public record CreateAppointmentStatusRequest(
    string Name,
    string Description
);

public record CreateAppointmentStatusResponse(
    Guid Id
);

public class CreateAppointmentStatusEndpoint : Endpoint<CreateAppointmentStatusRequest, CreateAppointmentStatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateAppointmentStatusEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/appointment-statuses");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentStatuses"));
    }

    public override async Task HandleAsync(CreateAppointmentStatusRequest request, CancellationToken cancellationToken)
    {
        var appointmentStatus = new AppointmentStatus
        {
            Name = request.Name,
            Description = request.Description,
        };

        _context.AppointmentStatuses.Add(appointmentStatus);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new CreateAppointmentStatusResponse(appointmentStatus.Id),
            cancellation: cancellationToken);
    }
}

public sealed class CreateAppointmentStatusValidator : Validator<CreateAppointmentStatusRequest>
{
    public CreateAppointmentStatusValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}