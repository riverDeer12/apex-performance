using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record GetBodyMeasurementsResponse(
    Guid Id,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    BodyMeasurementClientDto Client);

public class GetBodyMeasurementsEndpoint : EndpointWithoutRequest<List<GetBodyMeasurementsResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetBodyMeasurementsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/body-measurements");
        Permissions(nameof(UserPermissions.CanGetBodyMeasurements));
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var bodyMeasurements = await _context.BodyMeasurements
            .Include(x => x.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (bodyMeasurements.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }
        
        await SendAsync(bodyMeasurements
            .Select(x => 
                new GetBodyMeasurementsResponse(x.Id, x.Height, x.Weight, x.Shoulders, x.Chest, x.UpperArm, x.Waist, x.Thigh, x.Calves, 
                    new BodyMeasurementClientDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)))
            .ToList(), cancellation: cancellationToken);
    }
}