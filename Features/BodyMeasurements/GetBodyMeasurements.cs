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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    BodyMeasurementClientDto Client);

public record BodyMeasurementsByDayDto(
    DateTimeOffset Day,
    List<GetBodyMeasurementsResponse> BodyMeasurements
);

public class GetBodyMeasurementsEndpoint : EndpointWithoutRequest<List<BodyMeasurementsByDayDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetBodyMeasurementsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/body-measurements/by-day");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var bodyMeasurements = await _context.BodyMeasurements
            .Where(x => !x.IsDeleted)
            .Include(x => x.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (bodyMeasurements.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var bodyMeasurementsList = bodyMeasurements
            .Select(x =>
                new GetBodyMeasurementsResponse(x.Id, x.Height, x.Weight, x.Shoulders, x.Chest, x.UpperArm, x.Waist,
                    x.Thigh, x.Calves, x.CreatedAt, x.UpdatedAt,
                    new BodyMeasurementClientDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)))
            .ToList();

        var bodyMeasurementsByDay = GroupBodyMeasurementsByDay(bodyMeasurementsList);

        await SendAsync(bodyMeasurementsByDay, cancellation: cancellationToken);
    }
    
    private List<BodyMeasurementsByDayDto> GroupBodyMeasurementsByDay(List<GetBodyMeasurementsResponse> bodyMeasurementsList)
    {
        var itemsByDay = bodyMeasurementsList
            .GroupBy(item => item.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        return itemsByDay.Select(x => new BodyMeasurementsByDayDto(x.Key, x.Value)).ToList();
    }
}