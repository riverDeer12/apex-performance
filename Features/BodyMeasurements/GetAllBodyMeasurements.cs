using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public class GetAllBodyMeasurementsEndpoint : EndpointWithoutRequest<List<GetBodyMeasurementsResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAllBodyMeasurementsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/body-measurements");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
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
                new GetBodyMeasurementsResponse(x.Id, x.Height, x.Weight, x.Shoulders, x.Chest, x.UpperArm, x.Waist,
                    x.Thigh, x.Calves, x.CreatedAt, x.UpdatedAt,
                    new BodyMeasurementClientDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)))
            .ToList(), cancellation: cancellationToken);
    }
}