using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentTypes;

public record GetAllAppointmentTypeResponse(
    Guid Id,
    string Name
);

public class GetAllAppointmentTypesEndpoint : EndpointWithoutRequest<List<GetAllAppointmentTypeResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAllAppointmentTypesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-types/all");
        Options(x => x.WithTags("AppointmentTypes"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentTypes = await _context.AppointmentTypes
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointmentTypes.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(appointmentTypes
            .Select(x => new GetAllAppointmentTypeResponse(x.Id, x.Name))
            .ToList(), cancellation: cancellationToken);
    }
}