using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
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
    List<CatalogDataDto> WorkoutTypes
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

        await SendAsync(
            workouts.Select(workout => new GetWorkoutResponse(workout.Id, new LocalizedProperty(workout.Name),
                new LocalizedProperty(workout.Description), workout.ThumbnailUrl,
                workout.VideoUrl, workout.WorkoutTypes.Select(workoutTypeRelation =>
                    new CatalogDataDto(workoutTypeRelation.WorkoutType.Id, workoutTypeRelation.WorkoutType.Name,
                        workoutTypeRelation.WorkoutType.Description)).ToList())).ToList(),
            cancellation: cancellationToken);
    }
}