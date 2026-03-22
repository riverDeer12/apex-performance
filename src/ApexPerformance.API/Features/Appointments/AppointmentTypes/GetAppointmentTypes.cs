using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentTypes;

public class GetAppointmentTypesEndpoint : EndpointWithoutRequest<List<CatalogDataDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentTypesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-types");
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
            .Select(x => new CatalogDataDto(x.Id, x.Name, x.Description))
            .ToList(), cancellation: cancellationToken);
    }
}