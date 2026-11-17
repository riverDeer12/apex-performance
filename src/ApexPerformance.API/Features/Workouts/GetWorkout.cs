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

        if (!await WorkoutFavorites.CanSee(_context, _currentUserService, workout, cancellationToken))
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var isFavorite = await WorkoutFavorites.IsFavorite(_context, _currentUserService.UserId, id,
            cancellationToken);

        await SendAsync(WorkoutMapper.ToResponse(workout, isFavorite), cancellation: cancellationToken);
    }
}

public static class WorkoutMapper
{
    /// <summary>
    /// Workout types need to be loaded.
    /// </summary>
    public static GetWorkoutResponse ToResponse(Workout workout, bool isFavorite = false)
        => new(workout.Id, new LocalizedProperty(workout.Name), new LocalizedProperty(workout.Description),
            workout.ThumbnailUrl, workout.VideoUrl,
            (workout.WorkoutTypes ?? []).Where(x => x.WorkoutType is not null)
                .Select(x => new CatalogDataDto(x.WorkoutType.Id, x.WorkoutType.Name, x.WorkoutType.Description))
                .ToList(),
            WorkoutMuscles.Parse(workout.Muscles), WorkoutMuscles.ParseSecondary(workout), workout.IsTimed,
            workout.Category, workout.PaceType, isFavorite);
}
