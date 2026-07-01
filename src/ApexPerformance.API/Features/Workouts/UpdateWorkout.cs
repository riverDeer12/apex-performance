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
    private readonly IConfiguration _configuration;

    public UpdateWorkoutEndpoint(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        workout.Name = await LocalizedProperty.PopulateMissingLanguages(
            _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
            Language.HR, request.Name.Get(Language.HR));
        workout.Description = await LocalizedProperty.PopulateMissingLanguages(
            _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
            Language.HR, request.Description.Get(Language.HR));
        workout.ThumbnailUrl = YoutubeHelper.GetYoutubeThumbnail(request.VideoUrl);
        workout.VideoUrl = request.VideoUrl;
        
        workout.WorkoutTypes.Clear();

        foreach (var x in request.WorkoutTypes)
        {
            workout.WorkoutTypes.Add(new WorkoutWorkoutType
            {
                WorkoutTypeId = x
            });
        }

        _context.Workouts.Update(workout);
        
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
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.ThumbnailUrl).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.VideoUrl).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}