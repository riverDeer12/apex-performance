using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Workouts;

public record CreateWorkoutRequest(
    LocalizedProperty Name,
    LocalizedProperty Description,
    string ThumbnailUrl,
    string VideoUrl
    );

public class CreateWorkoutEndpoint : Endpoint<CreateWorkoutRequest, GetWorkoutResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateWorkoutEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/workouts");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CreateWorkoutRequest request, CancellationToken cancellationToken)
    {
        var workout = new Workout
        {
            Name = request.Name.ToJsonString(),
            Description = request.Description.ToJsonString(),
            ThumbnailUrl = request.ThumbnailUrl,
            VideoUrl = request.VideoUrl,
            WorkoutType = null,
            WorkoutTypeId = default
        };

        _context.Workouts.Add(workout);
        
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);
        
        await SendAsync(new GetWorkoutResponse(workout.Id, new LocalizedProperty(workout.Name),
                new LocalizedProperty(workout.Description), workout.ThumbnailUrl,
                workout.VideoUrl), cancellation: cancellationToken);
    }
}

public sealed class CreateWorkoutValidator : Validator<CreateWorkoutRequest>
{
    public CreateWorkoutValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.ThumbnailUrl).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.VideoUrl).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}