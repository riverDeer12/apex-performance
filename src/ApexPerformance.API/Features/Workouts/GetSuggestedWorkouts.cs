using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record GetSuggestedWorkoutsRequest(string? Name);

/// <summary>
/// Ids of the workouts that fit a training with the given name, best first
/// (WorkoutSuggestions). The apps show them on top when adding exercises.
/// </summary>
public class GetSuggestedWorkoutsEndpoint : Endpoint<GetSuggestedWorkoutsRequest, List<Guid>>
{
    // Enough earlier trainings to learn from without loading all of them.
    private const int PastTrainingsCount = 1000;

    private readonly ApexPerformanceContext _context;

    public GetSuggestedWorkoutsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/workouts/suggested");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(GetSuggestedWorkoutsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var workouts = (await _context.Workouts
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id, x.Name, x.Muscles, x.SecondaryMuscles, x.Category,
                    TypeNames = x.WorkoutTypes.Select(type => type.WorkoutType.Name).ToList()
                })
                .ToListAsync(cancellationToken))
            .Select(x => new WorkoutSuggestions.WorkoutInfo(x.Id, AllTranslations(x.Name),
                x.TypeNames.Select(AllTranslations).ToList(),
                WorkoutMuscles.Parse(x.Muscles), WorkoutMuscles.Parse(x.SecondaryMuscles), x.Category))
            .ToList();

        var pastTrainings = (await _context.Trainings
                .AsNoTracking()
                .OrderByDescending(x => x.Date)
                .Take(PastTrainingsCount)
                .Select(x => new { x.Name, WorkoutIds = x.Exercises.Select(exercise => exercise.WorkoutId).ToList() })
                .ToListAsync(cancellationToken))
            .Select(x => new WorkoutSuggestions.PastTraining(x.Name, x.WorkoutIds))
            .ToList();

        var templates = (await _context.TrainingTemplates
                .AsNoTracking()
                .Select(x => new { x.Name, WorkoutIds = x.Exercises.Select(exercise => exercise.WorkoutId).ToList() })
                .ToListAsync(cancellationToken))
            .Select(x => new WorkoutSuggestions.PastTraining(x.Name, x.WorkoutIds));

        await SendAsync(WorkoutSuggestions.Suggest(request.Name, workouts, pastTrainings.Concat(templates)),
            cancellation: cancellationToken);
    }

    // Names are stored as JSON of translations; every language is matched.
    private static string AllTranslations(string localizedJson)
        => string.Join(' ', new LocalizedProperty(localizedJson).Translations.Values);
}
