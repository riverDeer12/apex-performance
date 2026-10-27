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
    string? ThumbnailUrl,
    // Optional, video can be added later.
    string? VideoUrl,
    List<Guid> WorkoutTypes,
    // Codes of WorkoutMuscles, primary and secondary. Null keeps the
    // current ones (guessed from the name for a new workout).
    List<string>? Muscles = null,
    List<string>? SecondaryMuscles = null,
    // Sets record a duration (e.g. plank). Null keeps the current value.
    bool? IsTimed = null,
    // WorkoutCategories. Null keeps the current one (from the workout
    // types for a new workout).
    string? Category = null,
    // WorkoutPaceTypes, sets then record a distance. Null keeps the current value,
    // "" records no distance.
    string? PaceType = null
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
        // Clients can only view workouts.
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
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

        var existingWorkoutKeys = await WorkoutDuplicates.GetExistingKeys(_context, workoutId,
            cancellationToken);

        if (existingWorkoutKeys.Contains(WorkoutDuplicates.GetKey(request.Name.Get(Language.HR),
                request.Description.Get(Language.HR))))
            ThrowError(ErrorCodes.AlreadyExists);

        var workoutTypeIds = (request.WorkoutTypes ?? []).Distinct().ToList();

        var workoutTypes = await _context.WorkoutTypes
            .Where(x => workoutTypeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (workoutTypes.Count != workoutTypeIds.Count)
            ThrowError(ErrorCodes.NotValid);

        workout.Name = request.Name.ToJsonString();
        workout.Description = request.Description.ToJsonString();
        workout.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl)
            ? YoutubeHelper.GetThumbnailOrEmpty(request.VideoUrl)
            : request.ThumbnailUrl.Trim();
        workout.VideoUrl = request.VideoUrl?.Trim() ?? string.Empty;

        WorkoutMuscles.Apply(workout, request.Muscles, request.SecondaryMuscles);

        if (request.IsTimed is not null)
            workout.IsTimed = request.IsTimed.Value;

        if (request.PaceType is not null)
            workout.PaceType = request.PaceType.Length == 0 ? null : request.PaceType;

        // Pace comes from distance and duration, so these sets record a duration too.
        if (workout.PaceType is not null) workout.IsTimed = true;

        if (request.Category is not null)
            workout.Category = request.Category;

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
                        workoutTypeRelation.WorkoutType.Description)).ToList(),
                WorkoutMuscles.Parse(workout.Muscles), WorkoutMuscles.ParseSecondary(workout), workout.IsTimed,
                workout.Category, workout.PaceType),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateWorkoutValidator : Validator<UpdateWorkoutRequest>
{
    public UpdateWorkoutValidator()
    {
        RuleForEach(x => x.Muscles).Must(WorkoutMuscles.IsValid).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Category).Must(WorkoutCategories.IsValid).WithMessage(ErrorCodes.NotValid)
            .When(x => x.Category is not null);
        RuleFor(x => x.PaceType).Must(x => x == "" || WorkoutPaceTypes.All.Contains(x)).WithMessage(ErrorCodes.NotValid)
            .When(x => x.PaceType is not null);
        RuleForEach(x => x.SecondaryMuscles).Must(WorkoutMuscles.IsValid).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Name.Get(Language.HR)).NotEmpty().WithMessage(ErrorCodes.Required)
            .When(x => x.Name is not null);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Description.Get(Language.HR)).NotEmpty().WithMessage(ErrorCodes.Required)
            .When(x => x.Description is not null);
        RuleFor(x => x.VideoUrl).Must(x => YoutubeHelper.TryExtractVideoId(x, out _))
            .WithMessage(ErrorCodes.NotValid)
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));
    }
}