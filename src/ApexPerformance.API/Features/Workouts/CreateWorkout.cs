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

public record CreateWorkoutRequest(
    LocalizedProperty Name,
    LocalizedProperty Description,
    string ThumbnailUrl,
    string VideoUrl,
    List<Guid> WorkoutTypes
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
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
    }

    public override async Task HandleAsync(CreateWorkoutRequest request, CancellationToken cancellationToken)
    {
        var existingWorkoutKeys = await WorkoutDuplicates.GetExistingKeys(_context,
            cancellationToken: cancellationToken);

        if (existingWorkoutKeys.Contains(WorkoutDuplicates.GetKey(request.Name.Get(Language.HR),
                request.Description.Get(Language.HR))))
            ThrowError(ErrorCodes.AlreadyExists);

        var workoutTypeIds = (request.WorkoutTypes ?? []).Distinct().ToList();

        // Workout types are loaded so the response
        // can contain their names and unknown ids
        // are rejected before saving.
        var workoutTypes = await _context.WorkoutTypes
            .Where(x => workoutTypeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (workoutTypes.Count != workoutTypeIds.Count)
            ThrowError(ErrorCodes.NotValid);

        var workout = new Workout
        {
            Name = request.Name.ToJsonString(),
            Description = request.Description.ToJsonString(),
            ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl)
                ? YoutubeHelper.GetYoutubeThumbnail(request.VideoUrl)
                : request.ThumbnailUrl.Trim(),
            VideoUrl = request.VideoUrl.Trim(),
            WorkoutTypes = workoutTypes.Select(x => new WorkoutWorkoutType
            {
                WorkoutType = x
            }).ToList()
        };

        _context.Workouts.Add(workout);

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

public sealed class CreateWorkoutValidator : Validator<CreateWorkoutRequest>
{
    public CreateWorkoutValidator()
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