using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record GetBodyMeasurementsByClientResponse(
    Guid Id,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public class GetBodyMeasurementsByClientEndpoint : EndpointWithoutRequest<List<GetBodyMeasurementsByClientResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetBodyMeasurementsByClientEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/body-measurements/client/{id}");
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clientId = Route<Guid>("id", isRequired: true);

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == clientId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var bodyMeasurements = await _context.BodyMeasurements
            .Where(x => x.ClientId == clientId && !x.IsDeleted)
            .Include(bodyMeasurement => bodyMeasurement.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (bodyMeasurements.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(bodyMeasurements
            .Select(x =>
                new GetBodyMeasurementsByClientResponse(x.Id, x.Height, x.Weight, x.Shoulders, x.Chest, x.UpperArm,
                    x.Waist,
                    x.Thigh, x.Calves, x.CreatedAt, x.UpdatedAt))
            .ToList(), cancellation: cancellationToken);
    }
}