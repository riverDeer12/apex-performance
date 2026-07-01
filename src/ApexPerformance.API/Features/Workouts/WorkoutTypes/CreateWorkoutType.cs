using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities.Catalog;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Workouts.WorkoutTypes;

public record CreateWorkoutTypeRequest(
    string Name,
    string Description
);

public record CreateWorkoutTypeResponse(
    Guid Id
);

public class CreateWorkoutTypeEndpoint : Endpoint<CreateWorkoutTypeRequest, CreateWorkoutTypeResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateWorkoutTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/workout-types");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("WorkoutTypes"));
    }

    public override async Task HandleAsync(CreateWorkoutTypeRequest request, CancellationToken cancellationToken)
    {
        var workoutType = new WorkoutType
        {
            Name = request.Name,
            Description = request.Description.ToLower(),
        };

        _context.WorkoutTypes.Add(workoutType);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new CreateWorkoutTypeResponse(workoutType.Id),
            cancellation: cancellationToken);
    }
}

public sealed class CreateWorkoutTypeValidator : Validator<CreateWorkoutTypeRequest>
{
    public CreateWorkoutTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}
