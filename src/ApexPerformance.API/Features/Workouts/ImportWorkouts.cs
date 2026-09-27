using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record ImportWorkoutsRequest(
    IFormFile File
);

public record ImportWorkoutsResponse(
    Guid Id,
    bool Status,
    int QueuedWorkoutsCount
);

/// <summary>
/// Result of background workouts import
/// that is sent to the user by email.
/// </summary>
public record WorkoutsImportResult(
    int CreatedWorkoutsCount,
    int SkippedWorkoutsCount,
    int CreatedWorkoutTypesCount,
    string? ErrorMessage = null
);

public class ImportWorkouts : Endpoint<ImportWorkoutsRequest, ImportWorkoutsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;

    public ImportWorkouts(ApexPerformanceContext context, IConfiguration configuration,
        ICurrentUserService currentUserService, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _emailService = emailService;
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

        // File is validated right away so user gets errors immediately,
        // translating and saving can take a while so it runs in background
        // and user is notified by email when it is finished.
        var userId = _currentUserService.UserId;

        BackgroundJob.Enqueue(() => ProcessImport(userId, rows));

        await SendAsync(new ImportWorkoutsResponse
        (
            Guid.NewGuid(),
            true,
            rows.Count
        ), cancellation: cancellationToken);
    }

    /// <summary>
    /// Background job that translates and saves
    /// imported workouts, and sends email with
    /// the result to user that started the import.
    /// Not retried, so user does not get multiple emails.
    /// </summary>
    /// <param name="userId">User that started the import.</param>
    /// <param name="rows">Validated rows from Excel file.</param>
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessImport(Guid userId, List<ExcelRow> rows)
    {
        WorkoutsImportResult result;

        try
        {
            result = await SaveWorkouts(userId, rows);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Workouts import failed: {ex}");

            result = new WorkoutsImportResult(0, 0, 0, ex.Message);
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is not null)
            _emailService.SendWorkoutsImportFinishedEmail(user, result);

        if (result.ErrorMessage is not null)
            throw new InvalidOperationException($"Workouts import failed: {result.ErrorMessage}");
    }

    private async Task<WorkoutsImportResult> SaveWorkouts(Guid userId, List<ExcelRow> rows)
    {
        var existingWorkoutNames = await GetExistingWorkoutNames(CancellationToken.None);
        var workoutTypes = await GetExistingWorkoutTypes(CancellationToken.None);
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
                // There is no logged user in background job,
                // so audit fields are set to user that started import.
                CreatedBy = userId,
                UpdatedBy = userId,
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

            var result = await _context.SaveChangesAsync();

            if (result == 0)
                throw new InvalidOperationException(ErrorCodes.SavingError);
        }

        return new WorkoutsImportResult(newWorkouts.Count, skippedWorkoutsCount, newWorkoutTypes.Count);
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
