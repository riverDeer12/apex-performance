using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record ImportWorkoutsRequest(
    IFormFile File
);

public record ImportWorkoutsResponse(
    Guid Id,
    bool Status,
    int CreatedWorkoutsCount,
    int SkippedWorkoutsCount,
    int CreatedWorkoutTypesCount
);

public class ImportWorkouts : Endpoint<ImportWorkoutsRequest, ImportWorkoutsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;

    public ImportWorkouts(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/workouts/import");
        Options(x => x.WithTags("Workouts"));
        AllowFileUploads();
    }

    public override async Task HandleAsync(ImportWorkoutsRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
            ThrowError(ErrorCodes.Required);

        if (!request.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            ThrowError(ErrorCodes.NotValid);

        List<ExcelRow> rows = null!;

        try
        {
            await using var stream = request.File.OpenReadStream();

            rows = ExcelLoader.Load(stream, null);
        }
        catch (Exception ex)
        {
            ThrowError(ErrorCodes.NotValid + " " + ex.Message);
        }

        ValidateRows(rows);

        var existingWorkoutNames = await GetExistingWorkoutNames(cancellationToken);
        var workoutTypes = await GetExistingWorkoutTypes(cancellationToken);
        var importedWorkoutNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var newWorkouts = new List<Workout>();
        var newWorkoutTypes = new List<WorkoutType>();
        var skippedWorkoutsCount = 0;

        foreach (var excelRow in rows)
        {
            // Skip workouts that already exist in db
            // or are repeated in the same file.
            if (existingWorkoutNames.Contains(excelRow.Name) || !importedWorkoutNames.Add(excelRow.Name))
            {
                skippedWorkoutsCount++;
                continue;
            }

            var rowWorkoutTypes = new List<WorkoutType>();

            foreach (var typeName in excelRow.WorkoutTypes)
            {
                if (!workoutTypes.TryGetValue(typeName, out var workoutType))
                {
                    var translatedTypeName = await Translate(typeName);

                    workoutType = new WorkoutType
                    {
                        Name = translatedTypeName,
                        Description = translatedTypeName
                    };

                    workoutTypes.Add(typeName, workoutType);
                    newWorkoutTypes.Add(workoutType);
                }

                if (!rowWorkoutTypes.Contains(workoutType))
                    rowWorkoutTypes.Add(workoutType);
            }

            newWorkouts.Add(new Workout
            {
                Name = await Translate(excelRow.Name),
                Description = await Translate(excelRow.Description),
                ThumbnailUrl = YoutubeHelper.GetYoutubeThumbnail(excelRow.VideoUrl),
                VideoUrl = excelRow.VideoUrl,
                WorkoutTypes = rowWorkoutTypes.Select(x => new WorkoutWorkoutType
                {
                    WorkoutType = x
                }).ToList()
            });
        }

        if (newWorkouts.Count > 0)
        {
            // New workout types and workouts are saved together
            // so failed import doesn't leave partial data in db.
            _context.WorkoutTypes.AddRange(newWorkoutTypes);
            _context.Workouts.AddRange(newWorkouts);

            var result = await _context.SaveChangesAsync(cancellationToken);

            if (result == 0)
                ThrowError(ErrorCodes.SavingError);
        }

        await SendAsync(new ImportWorkoutsResponse
        (
            Guid.NewGuid(),
            true,
            newWorkouts.Count,
            skippedWorkoutsCount,
            newWorkoutTypes.Count
        ), cancellation: cancellationToken);
    }

    /// <summary>
    /// Check that every row has name
    /// and valid YouTube video URL.
    /// All invalid rows are returned
    /// in one response.
    /// </summary>
    /// <param name="rows"></param>
    private void ValidateRows(List<ExcelRow> rows)
    {
        foreach (var excelRow in rows)
        {
            if (string.IsNullOrWhiteSpace(excelRow.Name))
                AddError($"{ErrorCodes.NotValid} Row {excelRow.RowNumber}: Name is required.");

            if (!YoutubeHelper.TryExtractVideoId(excelRow.VideoUrl, out _))
                AddError($"{ErrorCodes.NotValid} Row {excelRow.RowNumber}: VideoUrl is not a valid YouTube URL.");
        }

        ThrowIfAnyErrors();
    }

    /// <summary>
    /// Get croatian names of existing workouts
    /// for case-insensitive duplicate check.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<HashSet<string>> GetExistingWorkoutNames(CancellationToken cancellationToken)
    {
        var workoutNames = await _context.Workouts
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        return workoutNames
            .Select(x => new LocalizedProperty(x).Get(Language.HR)?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
    }

    /// <summary>
    /// Get existing workout types mapped
    /// by croatian name (case-insensitive).
    /// Types are tracked so new workouts
    /// can reference them directly.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, WorkoutType>> GetExistingWorkoutTypes(CancellationToken cancellationToken)
    {
        var workoutTypes = await _context.WorkoutTypes.ToListAsync(cancellationToken);

        var workoutTypesByName = new Dictionary<string, WorkoutType>(StringComparer.OrdinalIgnoreCase);

        foreach (var workoutType in workoutTypes)
        {
            var nameHr = new LocalizedProperty(workoutType.Name).Get(Language.HR)?.Trim();

            if (string.IsNullOrEmpty(nameHr)) continue;

            workoutTypesByName.TryAdd(nameHr, workoutType);
        }

        return workoutTypesByName;
    }

    private Task<string> Translate(string text)
        => LocalizedProperty.PopulateMissingLanguages(
            _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
            Language.HR, text);
}
