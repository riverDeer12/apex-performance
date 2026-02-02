using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record UpdateWorkoutRequest(
    LocalizedProperty Name,
    LocalizedProperty Description,
    string ThumbnailUrl,
    string VideoUrl
    );

public class UpdateWorkoutEndpoint : Endpoint<UpdateWorkoutRequest, GetWorkoutResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateWorkoutEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/workouts/{id}");
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(UpdateWorkoutRequest request, CancellationToken cancellationToken)
    {
        var workoutId = Route<Guid>("id", isRequired: true);

        var workout =
            await _context.Workouts
                .FirstOrDefaultAsync(x => x.Id == workoutId, cancellationToken: cancellationToken);

        if (workout is null)
            ThrowError(ErrorCodes.NotFound);

        workout.Name = request.Name.ToJsonString();
        workout.Description = request.Description.ToJsonString();
        workout.ThumbnailUrl = request.ThumbnailUrl;
        workout.VideoUrl = request.VideoUrl;

        _context.Workouts.Update(workout);
        
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);
        
        await SendAsync(new GetWorkoutResponse(workout.Id, new LocalizedProperty(workout.Name),
                new LocalizedProperty(workout.Description), workout.ThumbnailUrl,
                workout.VideoUrl), cancellation: cancellationToken);
    }
}

public sealed class UpdateWorkoutValidator : Validator<UpdateWorkoutRequest>
{
    public UpdateWorkoutValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.ThumbnailUrl).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.VideoUrl).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}