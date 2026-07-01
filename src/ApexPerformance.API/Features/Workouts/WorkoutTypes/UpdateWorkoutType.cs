using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts.WorkoutTypes;

public record UpdateWorkoutTypeRequest(
    string Name,
    string Description
);

public record UpdateWorkoutTypeResponse(
    Guid Id
);

public class UpdateWorkoutTypeEndpoint : Endpoint<UpdateWorkoutTypeRequest, UpdateWorkoutTypeResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateWorkoutTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/workout-types/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("WorkoutTypes"));
    }

    public override async Task HandleAsync(UpdateWorkoutTypeRequest request, CancellationToken cancellationToken)
    {
        var workoutTypeId = Route<Guid>("id", isRequired: true);

        var workoutType =
            await _context.WorkoutTypes
                .FirstOrDefaultAsync(x => x.Id == workoutTypeId, cancellationToken: cancellationToken);

        if (workoutType is null)
            ThrowError(ErrorCodes.NotFound);

        workoutType.Name = request.Name;
        workoutType.Description = request.Description.ToLower();

        _context.WorkoutTypes.Update(workoutType);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new UpdateWorkoutTypeResponse(workoutType.Id),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateWorkoutTypeValidator : Validator<UpdateWorkoutTypeRequest>
{
    public UpdateWorkoutTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}
