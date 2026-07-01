using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using FluentValidation;

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
    private readonly IConfiguration _configuration;

    public CreateWorkoutEndpoint(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
            Name =  await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, request.Name.Get(Language.HR)),
            Description = await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, request.Description.Get(Language.HR)),
            VideoUrl = request.VideoUrl,
            ThumbnailUrl = YoutubeHelper.GetYoutubeThumbnail(request.VideoUrl),
            WorkoutTypes = request.WorkoutTypes.Select(x => new WorkoutWorkoutType
            {
                WorkoutTypeId = x
            }).ToList(),
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
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.ThumbnailUrl).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.VideoUrl).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}