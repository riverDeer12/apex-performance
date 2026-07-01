using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts.WorkoutTypes;

public class GetWorkoutTypesEndpoint : EndpointWithoutRequest<List<CatalogDataDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetWorkoutTypesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/workout-types");
        Options(x => x.WithTags("WorkoutTypes"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workoutTypes = await _context.WorkoutTypes
            .ToListAsync(cancellationToken: cancellationToken);

        if (workoutTypes.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(workoutTypes
            .Select(x => new CatalogDataDto(x.Id, x.Name, x.Description))
            .ToList(), cancellation: cancellationToken);
    }
}
