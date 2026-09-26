using ApexPerformance.API.Database;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record GetWorkoutTypeResponse(
    Guid Id,
    LocalizedProperty Name,
    LocalizedProperty Description
);

public class GetWorkoutTypesEndpoint : EndpointWithoutRequest<List<GetWorkoutTypeResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetWorkoutTypesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/workout-types");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workoutTypes = await _context.WorkoutTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);

        await SendAsync(
            workoutTypes.Select(workoutType => new GetWorkoutTypeResponse(workoutType.Id,
                new LocalizedProperty(workoutType.Name),
                new LocalizedProperty(workoutType.Description))).ToList(),
            cancellation: cancellationToken);
    }
}
