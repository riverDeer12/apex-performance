using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

/// <summary>
/// Deletes a workout type and removes it from all workouts that have it,
/// the workouts themselves are kept. Types are shared by all coaches,
/// so only administrators can delete them.
/// </summary>
public class DeleteWorkoutTypeEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteWorkoutTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/workout-types/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workoutTypeId = Route<Guid>("id", isRequired: true);

        var workoutType = await _context.WorkoutTypes
            .FirstOrDefaultAsync(x => x.Id == workoutTypeId, cancellationToken);

        if (workoutType is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        // Links of deleted (hidden) workouts are removed too, the database
        // doesn't allow deleting a type that is still linked to any workout.
        var relations = await _context.WorkoutWorkoutTypes
            .IgnoreQueryFilters()
            .Where(x => x.WorkoutTypeId == workoutTypeId)
            .ToListAsync(cancellationToken);

        var removedFromWorkouts = relations.Count;

        _context.WorkoutWorkoutTypes.RemoveRange(relations);
        _context.WorkoutTypes.Remove(workoutType);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        Logger.LogInformation("Workout type {WorkoutTypeId} {WorkoutTypeName} deleted, removed from {Count} workouts",
            workoutType.Id, workoutType.Name, removedFromWorkouts);

        await SendAsync(new StatusResponse(workoutType.Id, true), cancellation: cancellationToken);
    }
}
