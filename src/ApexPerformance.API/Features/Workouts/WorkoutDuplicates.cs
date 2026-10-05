using System.Text.RegularExpressions;
using ApexPerformance.API.Database;
using ApexPerformance.API.Utilities.Localization;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

/// <summary>
/// Workout is duplicate if it has same croatian name
/// and description as another workout, ignoring
/// letter case and extra whitespace.
/// </summary>
public static class WorkoutDuplicates
{
    public static string GetKey(string? name, string? description)
        => $"{Normalize(name)}\u001f{Normalize(description)}";

    /// <summary>
    /// Get duplicate check keys of existing workouts.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="excludedWorkoutId">Workout that is ignored (e.g. one that is being updated).</param>
    /// <param name="cancellationToken"></param>
    public static async Task<HashSet<string>> GetExistingKeys(ApexPerformanceContext context,
        Guid? excludedWorkoutId = null, CancellationToken cancellationToken = default,
        Language language = Language.HR)
    {
        var workouts = await context.Workouts
            .AsNoTracking()
            .Where(x => excludedWorkoutId == null || x.Id != excludedWorkoutId)
            .Select(x => new { x.Name, x.Description })
            .ToListAsync(cancellationToken);

        return workouts
            .Select(x => GetKey(
                new LocalizedProperty(x.Name).Get(language),
                new LocalizedProperty(x.Description).Get(language)))
            .ToHashSet();
    }

    private static string Normalize(string? value)
        => Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim().ToLowerInvariant();
}
