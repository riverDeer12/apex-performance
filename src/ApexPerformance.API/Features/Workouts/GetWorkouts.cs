using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record GetWorkoutResponse(
    Guid Id,
    LocalizedProperty Name,
    LocalizedProperty Description,
    string ThumbnailUrl,
    string VideoUrl,
    List<CatalogDataDto> WorkoutTypes,
    // Codes of WorkoutMuscles, for the body map: primary ones
    // doing most of the work and secondary ones helping.
    List<string> Muscles,
    List<string> SecondaryMuscles,
    // Sets of the workout record a duration (e.g. plank).
    bool IsTimed,
    // WorkoutCategories, for filtering the list.
    string Category,
    // WorkoutPaceTypes: sets record a distance and show a pace. Null for other workouts.
    string? PaceType = null,
    // The logged in user marked it with the star. Missing in older servers' responses.
    bool IsFavorite = false
);

public class GetWorkoutsEndpoint : EndpointWithoutRequest<List<GetWorkoutResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkoutsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/workouts");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workouts = await _context.Workouts
            .Include(workout => workout.WorkoutTypes)
            .ThenInclude(workoutWorkoutType => workoutWorkoutType.WorkoutType)
            .ToListAsync(cancellationToken: cancellationToken);

        // Clients only see the workouts their plan includes.
        var planFilter = await WorkoutPlanAccess.GetFilter(_context, _currentUserService, cancellationToken);

        if (planFilter is not null)
            workouts = workouts.Where(planFilter).ToList();

        if (workouts.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var favoriteIds = await WorkoutFavorites.IdsOf(_context, _currentUserService.UserId, cancellationToken);

        await SendAsync(WorkoutFavorites.Mark(workouts.Select(workout => WorkoutMapper.ToResponse(workout)),
                favoriteIds),
            cancellation: cancellationToken);
    }
}