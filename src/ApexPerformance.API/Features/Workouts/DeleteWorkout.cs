using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public class DeleteWorkoutEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteWorkoutEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/workouts/{id}");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workoutId = Route<Guid>("id", isRequired: true);

        var workout =
            await _context.Workouts
                .FirstOrDefaultAsync(x => x.Id == workoutId, 
                    cancellationToken: cancellationToken);

        if (workout is null)
            ThrowError(ErrorCodes.NotFound);

        workout.Delete();

        _context.Workouts.Update(workout);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new StatusResponse(workout.Id, true), cancellation: cancellationToken);
    }
}