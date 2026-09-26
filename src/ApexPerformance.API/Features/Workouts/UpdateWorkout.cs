using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record UpdateWorkoutRequest(
    LocalizedProperty Name,
    LocalizedProperty Description,
    string ThumbnailUrl,
    string VideoUrl,
    List<Guid> WorkoutTypes
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
                .Include(workout => workout.WorkoutTypes)
                .ThenInclude(workoutWorkoutType => workoutWorkoutType.WorkoutType)
                .FirstOrDefaultAsync(x => x.Id == workoutId, cancellationToken: cancellationToken);

        if (workout is null)
            ThrowError(ErrorCodes.NotFound);

        var workoutTypeIds = (request.WorkoutTypes ?? []).Distinct().ToList();

        var workoutTypes = await _context.WorkoutTypes
            .Where(x => workoutTypeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (workoutTypes.Count != workoutTypeIds.Count)
            ThrowError(ErrorCodes.NotValid);

        workout.Name = request.Name.ToJsonString();
        workout.Description = request.Description.ToJsonString();
        workout.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl)
            ? YoutubeHelper.GetYoutubeThumbnail(request.VideoUrl)
            : request.ThumbnailUrl.Trim();
        workout.VideoUrl = request.VideoUrl.Trim();

        // Only relations that changed are removed or added
        // so unchanged ones are not deleted and re-inserted
        // with the same composite key.
        foreach (var relation in workout.WorkoutTypes
                     .Where(x => !workoutTypeIds.Contains(x.WorkoutTypeId)).ToList())
        {
            _context.WorkoutWorkoutTypes.Remove(relation);
            workout.WorkoutTypes.Remove(relation);
        }

        foreach (var workoutType in workoutTypes
                     .Where(x => workout.WorkoutTypes.All(relation => relation.WorkoutTypeId != x.Id)))
        {
            workout.WorkoutTypes.Add(new WorkoutWorkoutType
            {
                WorkoutType = workoutType
            });
        }

        // Only the workout itself is marked as modified,
        // relations are already tracked from the query above.
        _context.Entry(workout).State = EntityState.Modified;
        
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);
        
        await SendAsync(new GetWorkoutResponse(workout.Id, new LocalizedProperty(workout.Name),
                new LocalizedProperty(workout.Description), workout.ThumbnailUrl,
                workout.VideoUrl, workout.WorkoutTypes.Select(workoutTypeRelation =>
                    new CatalogDataDto(workoutTypeRelation.WorkoutType.Id, workoutTypeRelation.WorkoutType.Name,
                        workoutTypeRelation.WorkoutType.Description)).ToList()),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateWorkoutValidator : Validator<UpdateWorkoutRequest>
{
    public UpdateWorkoutValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Name.Get(Language.HR)).NotEmpty().WithMessage(ErrorCodes.Required)
            .When(x => x.Name is not null);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Description.Get(Language.HR)).NotEmpty().WithMessage(ErrorCodes.Required)
            .When(x => x.Description is not null);
        RuleFor(x => x.VideoUrl).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.VideoUrl).Must(x => YoutubeHelper.TryExtractVideoId(x, out _))
            .WithMessage(ErrorCodes.NotValid)
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));
    }
}