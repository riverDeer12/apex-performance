using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record MyAppointmentRequestResponse(
    AppointmentDataDto Appointment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>
/// Appointments the logged client requested (created) themselves,
/// with their current status, so the client can follow whether
/// each request is pending, approved or declined.
/// </summary>
public class GetMyAppointmentRequestsEndpoint : EndpointWithoutRequest<List<MyAppointmentRequestResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyAppointmentRequestsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointments/my-requests");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        // CreatedBy is set to the logged user when the appointment is saved.
        var appointments = await _context.Appointments
            .Where(x => x.CreatedBy == _currentUserService.UserId)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .Include(appointment => appointment.TimeSlot)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = appointments.Select(appointment =>
            new MyAppointmentRequestResponse(
                new AppointmentDataDto(appointment.Id,
                    appointment.StartTime, appointment.EndTime,
                    new CatalogDataDto(appointment.AppointmentType.Id,
                        appointment.AppointmentType.Name, appointment.AppointmentType.Description),
                    new CatalogDataDto(appointment.AppointmentStatus.Id,
                        appointment.AppointmentStatus.Name, appointment.AppointmentStatus.Description),
                    appointment.Clients
                        .Select(x => new PersonDataDto(x.Client.Id, x.Client.FirstName,
                            x.Client.LastName, x.Client.FullName))
                        .ToList(),
                    appointment.Coaches
                        .Select(x => new PersonDataDto(x.CoachId, x.Coach.FirstName,
                            x.Coach.LastName, x.Coach.FullName))
                        .ToList(),
                    new CatalogDataDto(appointment.TimeSlot.Id, appointment.TimeSlot.Name,
                        appointment.TimeSlot.Description)),
                appointment.CreatedAt,
                appointment.UpdatedAt))
            .ToList();

        await SendAsync(response, cancellation: cancellationToken);
    }
}
