using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

/// <summary>
/// One workout, e.g. opened from an exercise of a training. Clients get
/// the workouts their plan includes and the ones in their trainings.
/// </summary>
public class GetWorkoutEndpoint : EndpointWithoutRequest<GetWorkoutResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkoutEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/workouts/{id}");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var workout = await _context.Workouts
            .Include(x => x.WorkoutTypes)
            .ThenInclude(x => x.WorkoutType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (workout is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var planFilter = await WorkoutPlanAccess.GetFilter(_context, _currentUserService, cancellationToken);

        if (planFilter is not null && !planFilter(workout))
        {
            var userId = _currentUserService.UserId;
            var isInTraining = await _context.Trainings.AnyAsync(training =>
                training.Client.UserId == userId &&
                training.Exercises.Any(exercise => exercise.WorkoutId == id), cancellationToken);

            if (!isInTraining)
                ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);
        }

        await SendAsync(WorkoutMapper.ToResponse(workout), cancellation: cancellationToken);
    }
}

public static class WorkoutMapper
{
    /// <summary>
    /// Workout types need to be loaded.
    /// </summary>
    public static GetWorkoutResponse ToResponse(Workout workout)
        => new(workout.Id, new LocalizedProperty(workout.Name), new LocalizedProperty(workout.Description),
            workout.ThumbnailUrl, workout.VideoUrl,
            (workout.WorkoutTypes ?? []).Where(x => x.WorkoutType is not null)
                .Select(x => new CatalogDataDto(x.WorkoutType.Id, x.WorkoutType.Name, x.WorkoutType.Description))
                .ToList(),
            WorkoutMuscles.Parse(workout.Muscles), WorkoutMuscles.ParseSecondary(workout), workout.IsTimed,
            workout.Category, workout.PaceType);
}
