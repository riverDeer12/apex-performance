using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record ImportWorkoutsRequest(
    IFormFile File
);

public class ImportWorkouts : Endpoint<ImportWorkoutsRequest, StatusResponse>
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
        if (request.File.Length == 0)
            ThrowError(ErrorCodes.NotFound);

        if (!request.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            ThrowError(ErrorCodes.NotFound);

        List<ExcelRow> rows = null;

        try
        {
            await using var stream = request.File.OpenReadStream();

            rows = ExcelLoader.Load(stream, null);
        }
        catch (Exception ex)
        {
            ThrowError(ErrorCodes.NotValid + " " + ex.Message);
        }

        rows = RemoveWorkoutDuplicates(rows);

        await CheckExcelWorkoutTypes(rows, cancellationToken);

        rows = await SetWorkoutTypes(rows);

        var newWorkouts = await ProcessNewWorkouts(rows);

        if (newWorkouts.Count is 0)
        {
            await SendAsync(new StatusResponse
            (
                Guid.NewGuid(),
                true
            ), cancellation: cancellationToken);
            return;
        }

        _context.Workouts.AddRange(newWorkouts);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new StatusResponse
        (
            Guid.NewGuid(),
            true
        ), cancellation: cancellationToken);
    }

    private async Task<List<Workout>> ProcessNewWorkouts(List<ExcelRow> rows)
    {
        var newWorkouts = new List<Workout>();
        
        var existingWorkouts = (await _context.Workouts
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name })
                .ToListAsync())
            .Select(x => new
            {
                x.Id,
                NameHr = new LocalizedProperty(x.Name).Get(Language.HR)
            })
            .ToList();

        foreach (var excelRow in rows)
        {
            var relatedWorkout = existingWorkouts.FirstOrDefault(x =>
                x.NameHr.Equals(excelRow.Name, StringComparison.OrdinalIgnoreCase));

            if (relatedWorkout != null) continue;
            
            var workoutName = await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, excelRow.Name);

            var workoutDescription = await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, excelRow.Description);

            var newWorkout = new Workout
            {
                Name = workoutName,
                Description = workoutDescription,
                ThumbnailUrl = YoutubeHelper.GetYoutubeThumbnail(excelRow.VideoUrl),
                VideoUrl = excelRow.VideoUrl,
                WorkoutTypes = excelRow.WorkoutTypes.Select(x => new WorkoutWorkoutType
                {
                    WorkoutTypeId = new Guid(x)
                }).ToList()
            };

            newWorkouts.Add(newWorkout);
        }

        return newWorkouts;
    }

    /// <summary>
    /// Check workout types that
    /// were assigned to workouts.
    /// If there is non-existent one than
    /// create it and save to db.
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task CheckExcelWorkoutTypes(List<ExcelRow> rows, CancellationToken cancellationToken)
    {
        var workoutTypes = new List<string>();

        foreach (var excelRow in rows)
        {
            foreach (var type in excelRow.WorkoutTypes)
            {
                if (workoutTypes.Contains(type)) continue;

                workoutTypes.Add(type);
            }
        }

        var workoutsNames = _context.WorkoutTypes
            .Select(x => new LocalizedProperty(x.Name))
            .ToList();

        var croatianWorkoutsTypesNames = workoutsNames
            .Select(x => x.Get(Language.HR)).ToList();

        workoutTypes = workoutTypes
            .Where(x => !croatianWorkoutsTypesNames
                .Contains(x.ToLower()))
            .ToList();

        await SaveNewWorkoutTypes(cancellationToken, workoutTypes);
    }

    /// <summary>
    /// Save new workout types to db.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="workoutTypes"></param>
    private async Task SaveNewWorkoutTypes(CancellationToken cancellationToken, List<string> workoutTypes)
    {
        var newWorkoutTypes = new List<WorkoutType>();

        foreach (var workoutType in workoutTypes)
        {
            var workoutTypeName = await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, workoutType);

            var workoutTypeDescription = await LocalizedProperty.PopulateMissingLanguages(
                _configuration["GoogleCloudConfiguration:TranslateServiceUrl"]!,
                Language.HR, workoutType);

            var newWorkoutType = new WorkoutType
            {
                Name = workoutTypeName,
                Description = workoutTypeDescription
            };

            newWorkoutTypes.Add(newWorkoutType);
        }

        if (newWorkoutTypes.Count is 0) return;

        _context.WorkoutTypes.AddRange(newWorkoutTypes);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);
    }

    /// <summary>
    /// Convert names from string list to related
    /// workout type guids from db.
    /// Goal is to have easier workouts
    /// save in db, while building m-m relation
    /// with workout types.
    /// </summary>
    /// <param name="rows"></param>
    /// <returns></returns>
    private async Task<List<ExcelRow>> SetWorkoutTypes(List<ExcelRow> rows)
    {
        var workoutTypes = (await _context.WorkoutTypes
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name })
                .ToListAsync())
            .Select(x => new
            {
                x.Id,
                NameHr = new LocalizedProperty(x.Name).Get(Language.HR)
            })
            .ToList();

        foreach (var excelRow in rows)
        {
            for (var i = 0; i < excelRow.WorkoutTypes.Count; i++)
            {
                var excelWorkoutType = excelRow.WorkoutTypes[i];

                var relatedWorkoutType = workoutTypes.FirstOrDefault(x =>
                    x.NameHr.Equals(excelWorkoutType, StringComparison.OrdinalIgnoreCase));

                if (relatedWorkoutType is null)
                    continue;

                excelRow.WorkoutTypes[i] = relatedWorkoutType.Id.ToString();
            }
        }

        return rows;
    }

    /// <summary>
    /// Remove possible workout duplicates
    /// and return clean list with unique
    /// workouts.
    /// </summary>
    /// <param name="rows"></param>
    /// <returns></returns>
    private List<ExcelRow> RemoveWorkoutDuplicates(List<ExcelRow> rows)
    {
        var workoutsNames = _context.Workouts
            .Select(x => new LocalizedProperty(x.Name))
            .ToList();

        var croatianWorkoutsNames = workoutsNames
            .Select(x => x.Get(Language.HR)).ToList();

        rows = rows
            .Where(r => !croatianWorkoutsNames.Contains(r.Name.ToLower()))
            .ToList();

        return rows;
    }
}