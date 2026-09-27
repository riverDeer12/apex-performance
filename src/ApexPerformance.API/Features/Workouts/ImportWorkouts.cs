using System.Text.RegularExpressions;
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
/// Excel row that was not saved, with reason.
/// Row values are kept so user can fix
/// and import them again.
/// </summary>
public record WorkoutsImportRowIssue(
    int RowNumber,
    string Name,
    string Description,
    string VideoUrl,
    string WorkoutTypes,
    string Reason
);

/// <summary>
/// Result of background workouts import
/// that is sent to the user by email.
/// </summary>
public record WorkoutsImportResult(
    int CreatedWorkoutsCount,
    int CreatedWorkoutTypesCount,
    List<WorkoutsImportRowIssue> FailedRows,
    List<WorkoutsImportRowIssue> SkippedRows,
    string? ErrorMessage = null
);

public class ImportWorkouts : Endpoint<ImportWorkoutsRequest, ImportWorkoutsResponse>
{
    // Name and Description columns are limited in db and
    // contain JSON with all translations, not only croatian text.
    private const int MaxLocalizedNameLength = 200;

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

        if (rows.Count == 0)
            ThrowError(ErrorCodes.Required);

        // Only file itself is checked here. Rows are validated,
        // translated and saved in background, invalid rows don't
        // stop valid ones from being saved and user gets email
        // with result and list of rows that were not saved.
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
    /// Background job that validates, translates and saves
    /// imported workouts, and sends email with the result
    /// to user that started the import.
    /// Not retried, so user does not get multiple emails.
    /// </summary>
    /// <param name="userId">User that started the import.</param>
    /// <param name="rows">Rows from Excel file.</param>
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

            result = new WorkoutsImportResult(0, 0, [], [], ex.Message);
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

        if (user is not null)
            _emailService.SendWorkoutsImportFinishedEmail(user, result);

        if (result.ErrorMessage is not null)
            throw new InvalidOperationException($"Workouts import failed: {result.ErrorMessage}");
    }

    private sealed record PreparedWorkout(ExcelRow Row, Workout Workout);

    private async Task<WorkoutsImportResult> SaveWorkouts(Guid userId, List<ExcelRow> rows)
    {
        var existingWorkoutKeys = await GetExistingWorkoutKeys();
        var workoutTypes = await GetExistingWorkoutTypes();
        var existingWorkoutTypes = workoutTypes.Values.ToHashSet();
        var importedWorkoutKeys = new HashSet<string>();

        var prepared = new List<PreparedWorkout>();
        var failedRows = new List<WorkoutsImportRowIssue>();
        var skippedRows = new List<WorkoutsImportRowIssue>();

        foreach (var excelRow in rows)
        {
            var validationError = ValidateRow(excelRow);

            if (validationError is not null)
            {
                failedRows.Add(ToIssue(excelRow, validationError));
                continue;
            }

            // Workout is duplicate if croatian name and description
            // match existing workout or workout from same file.
            var workoutKey = GetWorkoutKey(excelRow.Name, excelRow.Description);

            if (existingWorkoutKeys.Contains(workoutKey))
            {
                skippedRows.Add(ToIssue(excelRow, "Vježba s istim nazivom i opisom već postoji."));
                continue;
            }

            if (importedWorkoutKeys.Contains(workoutKey))
            {
                skippedRows.Add(ToIssue(excelRow, "Vježba s istim nazivom i opisom ponavlja se u datoteci."));
                continue;
            }

            try
            {
                var workout = await PrepareWorkout(userId, excelRow, workoutTypes);

                prepared.Add(new PreparedWorkout(excelRow, workout));
                importedWorkoutKeys.Add(workoutKey);
            }
            catch (Exception ex)
            {
                failedRows.Add(ToIssue(excelRow, ex.Message));
            }
        }

        var savedWorkouts = await SavePreparedWorkouts(prepared, existingWorkoutTypes, failedRows);

        var createdWorkoutTypesCount = savedWorkouts
            .SelectMany(x => x.WorkoutTypes.Select(relation => relation.WorkoutType))
            .Distinct()
            .Count(x => !existingWorkoutTypes.Contains(x));

        return new WorkoutsImportResult(
            savedWorkouts.Count,
            createdWorkoutTypesCount,
            failedRows.OrderBy(x => x.RowNumber).ToList(),
            skippedRows);
    }

    /// <summary>
    /// Check that row has name
    /// and valid YouTube video URL.
    /// </summary>
    /// <returns>Error message or null if row is valid.</returns>
    private static string? ValidateRow(ExcelRow excelRow)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(excelRow.Name))
            errors.Add("Naziv (Name) je obavezan.");

        if (string.IsNullOrWhiteSpace(excelRow.VideoUrl))
            errors.Add("YouTube poveznica (VideoUrl) je obavezna.");
        else if (!YoutubeHelper.TryExtractVideoId(excelRow.VideoUrl, out _))
            errors.Add("YouTube poveznica (VideoUrl) nije ispravna.");

        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    /// <summary>
    /// Translate row values and create workout
    /// with its workout types. New workout types are
    /// added to <paramref name="workoutTypes"/> only when
    /// whole row is prepared, so failed row doesn't leave them behind.
    /// </summary>
    private async Task<Workout> PrepareWorkout(Guid userId, ExcelRow excelRow,
        Dictionary<string, WorkoutType> workoutTypes)
    {
        var rowWorkoutTypes = new List<WorkoutType>();
        var rowNewWorkoutTypes = new Dictionary<string, WorkoutType>(StringComparer.OrdinalIgnoreCase);

        foreach (var typeName in excelRow.WorkoutTypes)
        {
            if (!workoutTypes.TryGetValue(typeName, out var workoutType) &&
                !rowNewWorkoutTypes.TryGetValue(typeName, out workoutType))
            {
                var translatedTypeName = await Translate(typeName, "vrste vježbe");

                if (translatedTypeName.Length > MaxLocalizedNameLength)
                    throw new InvalidOperationException($"Naziv vrste vježbe '{typeName}' je predugačak.");

                workoutType = new WorkoutType
                {
                    Name = translatedTypeName,
                    Description = translatedTypeName
                };

                rowNewWorkoutTypes.Add(typeName, workoutType);
            }

            if (!rowWorkoutTypes.Contains(workoutType))
                rowWorkoutTypes.Add(workoutType);
        }

        var name = await Translate(excelRow.Name, "naziva");

        if (name.Length > MaxLocalizedNameLength)
            throw new InvalidOperationException("Naziv (Name) je predugačak zajedno s prijevodima, skratite ga.");

        var description = string.IsNullOrWhiteSpace(excelRow.Description)
            ? new LocalizedProperty { Translations = { [Language.HR] = string.Empty } }.ToJsonString()
            : await Translate(excelRow.Description, "opisa");

        foreach (var (typeName, workoutType) in rowNewWorkoutTypes)
            workoutTypes.Add(typeName, workoutType);

        return new Workout
        {
            Name = name,
            Description = description,
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
        };
    }

    /// <summary>
    /// Save all prepared workouts at once. If that fails,
    /// workouts are saved one by one so valid ones are
    /// still saved and failed ones are added to <paramref name="failedRows"/>.
    /// </summary>
    /// <returns>Workouts that are saved.</returns>
    private async Task<List<Workout>> SavePreparedWorkouts(List<PreparedWorkout> prepared,
        HashSet<WorkoutType> existingWorkoutTypes, List<WorkoutsImportRowIssue> failedRows)
    {
        if (prepared.Count == 0) return [];

        try
        {
            _context.Workouts.AddRange(prepared.Select(x => x.Workout));

            await _context.SaveChangesAsync();

            return prepared.Select(x => x.Workout).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Saving imported workouts together failed, saving one by one: {ex}");
        }

        var savedWorkouts = new List<Workout>();
        var savedWorkoutTypes = new HashSet<WorkoutType>(existingWorkoutTypes);

        foreach (var item in prepared)
        {
            _context.ChangeTracker.Clear();

            item.Workout.Id = Guid.Empty;

            var itemWorkoutTypes = item.Workout.WorkoutTypes.Select(x => x.WorkoutType).ToList();

            foreach (var workoutType in itemWorkoutTypes)
            {
                // Relations from previous save attempt are removed so
                // other (maybe failed) workouts aren't reached from this graph.
                workoutType.Workouts = new List<WorkoutWorkoutType>();

                // Workout types that are already in db are only marked as unchanged,
                // new ones are inserted together with this workout.
                if (savedWorkoutTypes.Contains(workoutType))
                    _context.Entry(workoutType).State = EntityState.Unchanged;
                else
                    workoutType.Id = Guid.Empty;
            }

            item.Workout.WorkoutTypes = itemWorkoutTypes.Select(x => new WorkoutWorkoutType
            {
                WorkoutType = x
            }).ToList();

            try
            {
                _context.Workouts.Add(item.Workout);

                await _context.SaveChangesAsync();

                savedWorkouts.Add(item.Workout);

                foreach (var workoutType in item.Workout.WorkoutTypes.Select(x => x.WorkoutType))
                    savedWorkoutTypes.Add(workoutType);
            }
            catch (Exception ex)
            {
                failedRows.Add(ToIssue(item.Row,
                    $"Spremanje nije uspjelo: {ex.GetBaseException().Message}"));
            }
        }

        _context.ChangeTracker.Clear();

        return savedWorkouts;
    }

    private static WorkoutsImportRowIssue ToIssue(ExcelRow excelRow, string reason)
        => new(excelRow.RowNumber, excelRow.Name, excelRow.Description, excelRow.VideoUrl,
            string.Join(", ", excelRow.WorkoutTypes), reason);

    /// <summary>
    /// Key for duplicate check made from croatian
    /// name and description, ignoring letter case
    /// and extra whitespace.
    /// </summary>
    private static string GetWorkoutKey(string? name, string? description)
        => $"{Normalize(name)}\u001f{Normalize(description)}";

    private static string Normalize(string? value)
        => Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim().ToLowerInvariant();

    /// <summary>
    /// Get duplicate check keys (croatian name
    /// and description) of existing workouts.
    /// </summary>
    private async Task<HashSet<string>> GetExistingWorkoutKeys()
    {
        var workouts = await _context.Workouts
            .AsNoTracking()
            .Select(x => new { x.Name, x.Description })
            .ToListAsync();

        return workouts
            .Select(x => GetWorkoutKey(
                new LocalizedProperty(x.Name).Get(Language.HR),
                new LocalizedProperty(x.Description).Get(Language.HR)))
            .ToHashSet();
    }

    /// <summary>
    /// Get existing workout types mapped
    /// by croatian name (case-insensitive).
    /// Types are tracked so new workouts
    /// can reference them directly.
    /// </summary>
    private async Task<Dictionary<string, WorkoutType>> GetExistingWorkoutTypes()
    {
        var workoutTypes = await _context.WorkoutTypes.ToListAsync();

        var workoutTypesByName = new Dictionary<string, WorkoutType>(StringComparer.OrdinalIgnoreCase);

        foreach (var workoutType in workoutTypes)
        {
            var nameHr = new LocalizedProperty(workoutType.Name).Get(Language.HR)?.Trim();

            if (string.IsNullOrEmpty(nameHr)) continue;

            workoutTypesByName.TryAdd(nameHr, workoutType);
        }

        return workoutTypesByName;
    }

    private async Task<string> Translate(string text, string fieldName)
    {
        try
        {
            return await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Prijevod {fieldName} nije uspio: {ex.Message}", ex);
        }
    }
}
