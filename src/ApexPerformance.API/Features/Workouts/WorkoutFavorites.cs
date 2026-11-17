using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

/// <summary>
/// Workouts the logged in user marked with the star. Every coach and client
/// has their own; the workouts list and detail say which are favorites.
/// </summary>
public static class WorkoutFavorites
{
    /// <summary>
    /// Ids of the user's favorite workouts (deleted workouts are left out).
    /// </summary>
    public static async Task<HashSet<Guid>> IdsOf(ApexPerformanceContext context, Guid userId,
        CancellationToken cancellationToken)
        => (await context.FavoriteWorkouts
                .Where(x => x.UserId == userId)
                .Select(x => x.WorkoutId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

    public static Task<bool> IsFavorite(ApexPerformanceContext context, Guid userId, Guid workoutId,
        CancellationToken cancellationToken)
        => context.FavoriteWorkouts.AnyAsync(x => x.UserId == userId && x.WorkoutId == workoutId,
            cancellationToken);

    /// <summary>
    /// Responses with IsFavorite set from the user's favorite ids.
    /// </summary>
    public static List<GetWorkoutResponse> Mark(IEnumerable<GetWorkoutResponse> workouts,
        IReadOnlySet<Guid> favoriteIds)
        => workouts.Select(x => x with { IsFavorite = favoriteIds.Contains(x.Id) }).ToList();

    /// <summary>
    /// True when the logged in user may see the workout, the same as for opening it:
    /// staff see all, clients the workouts their plan includes and the ones in their trainings.
    /// </summary>
    public static async Task<bool> CanSee(ApexPerformanceContext context, ICurrentUserService currentUserService,
        Workout workout, CancellationToken cancellationToken)
    {
        var planFilter = await WorkoutPlanAccess.GetFilter(context, currentUserService, cancellationToken);

        if (planFilter is null || planFilter(workout))
            return true;

        var userId = currentUserService.UserId;
        return await context.Trainings.AnyAsync(training =>
            training.Client.UserId == userId &&
            training.Exercises.Any(exercise => exercise.WorkoutId == workout.Id), cancellationToken);
    }
}

/// <summary>
/// Marks a workout as a favorite of the logged in user. Marking it again changes nothing.
/// </summary>
public class AddFavoriteWorkoutEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddFavoriteWorkoutEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/workouts/{id}/favorite");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workoutId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        var workout = await _context.Workouts
            .Include(x => x.WorkoutTypes)
            .ThenInclude(x => x.WorkoutType)
            .FirstOrDefaultAsync(x => x.Id == workoutId, cancellationToken);

        if (workout is null || !await WorkoutFavorites.CanSee(_context, _currentUserService, workout,
                cancellationToken))
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        if (!await WorkoutFavorites.IsFavorite(_context, userId, workoutId, cancellationToken))
        {
            _context.FavoriteWorkouts.Add(new FavoriteWorkout
            {
                UserId = userId,
                WorkoutId = workoutId,
                CreatedAt = DateTimeOffset.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Marked at the same time from another device: already a favorite.
                if (!await WorkoutFavorites.IsFavorite(_context, userId, workoutId, CancellationToken.None))
                    throw;
            }
        }

        await SendNoContentAsync(cancellationToken);
    }
}

/// <summary>
/// Removes a workout from the favorites of the logged in user. Removing one that is not
/// a favorite changes nothing.
/// </summary>
public class RemoveFavoriteWorkoutEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFavoriteWorkoutEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/workouts/{id}/favorite");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workoutId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        await _context.FavoriteWorkouts
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.WorkoutId == workoutId)
            .ExecuteDeleteAsync(cancellationToken);

        await SendNoContentAsync(cancellationToken);
    }
}
