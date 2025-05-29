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
        var bodyMeasurements = await _context.BodyMeasurements.ToListAsync(cancellationToken: cancellationToken);

        if (bodyMeasurements.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }
        
        await SendAsync([], cancellation: cancellationToken);
    }
}