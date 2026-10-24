using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record GuessWorkoutMusclesRequest(
    LocalizedProperty Name,
    List<Guid> WorkoutTypes
);

public record GuessWorkoutMusclesResponse(
    // One of the workout types is for the whole body ("Full body").
    bool IsFullBody,
    List<string> Muscles,
    List<string> SecondaryMuscles
);

/// <summary>
/// Muscles of a workout being edited, guessed from its name the same way
/// as for a new workout without muscles. With a full body type, the muscles
/// of whole body exercises are marked too.
/// </summary>
public class GuessWorkoutMusclesEndpoint : Endpoint<GuessWorkoutMusclesRequest, GuessWorkoutMusclesResponse>
{
    private readonly ApexPerformanceContext _context;

    public GuessWorkoutMusclesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/workouts/guess-muscles");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(GuessWorkoutMusclesRequest request, CancellationToken cancellationToken)
    {
        var workoutTypeIds = (request.WorkoutTypes ?? []).Distinct().ToList();
        var workoutTypeNames = await _context.WorkoutTypes
            .AsNoTracking()
            .Where(x => workoutTypeIds.Contains(x.Id))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var isFullBody = workoutTypeNames.Any(WorkoutMuscles.IsFullBodyType);
        var guessed = WorkoutMuscles.Guess(request.Name.ToJsonString(), isFullBody);

        await SendAsync(new GuessWorkoutMusclesResponse(isFullBody, guessed.Primary, guessed.Secondary),
            cancellation: cancellationToken);
    }
}
