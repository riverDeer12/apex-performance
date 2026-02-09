using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record ImportWorkoutsRequest(
    IFormFile File
);

public class ImportWorkouts : Endpoint<ImportWorkoutsRequest, StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public ImportWorkouts(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/workouts/import");
        Options(x => x.WithTags("Workouts"));
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

        var newWorkouts = new List<Workout>();

        foreach (var excelRow in rows)
        {
            var newWorkout = new Workout
            {
                Name = excelRow.Name,
                Description = excelRow.Description,
                ThumbnailUrl = YoutubeHelper.GetYoutubeThumbnail(excelRow.VideoUrl),
                VideoUrl = excelRow.VideoUrl,
                WorkoutTypes = excelRow.WorkoutTypes.Select(x => new WorkoutWorkoutType
                {
                    WorkoutTypeId = new Guid(x)
                }).ToList()
            };
        }

        await SendAsync(new StatusResponse
        (
            Guid.NewGuid(),
            true
        ), cancellation: cancellationToken);
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

        var workoutsTypesNames = _context.WorkoutTypes
            .Select(x => x.Name.ToLower())
            .ToHashSet();

        workoutTypes = workoutTypes
            .Where(x => !workoutsTypesNames
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
            var newWorkoutType = new WorkoutType
            {
                Name = workoutType,
                Description = workoutType
            };

            newWorkoutTypes.Add(newWorkoutType);
        }

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
        var workoutTypes = await _context.WorkoutTypes
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .ToListAsync();

        foreach (var excelRow in rows)
        {
            for (var i = 0; i < excelRow.WorkoutTypes.Count; i++)
            {
                var excelWorkoutType = excelRow.WorkoutTypes[i];

                var relatedWorkoutType = workoutTypes.FirstOrDefault(x =>
                    x.Name.Equals(excelWorkoutType, StringComparison.OrdinalIgnoreCase));

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
            .Select(x => x.Name.ToLower())
            .ToHashSet();

        rows = rows
            .Where(r => !workoutsNames.Contains(r.Name.ToLower()))
            .ToList();

        return rows;
    }
}