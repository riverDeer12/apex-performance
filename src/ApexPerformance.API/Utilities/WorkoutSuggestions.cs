using System.Text.RegularExpressions;
using ApexPerformance.API.Constants;

namespace ApexPerformance.API.Utilities;

/// <summary>
/// Workouts that fit a training by its name, offered first when the coach
/// adds exercises: workouts used in earlier trainings and templates with
/// a similar name, workouts for the muscles or category the name mentions
/// (e.g. "Upper body", "Noge", "Mobilnost") and workouts whose name or type
/// shares a word with it (e.g. "Squat day").
/// </summary>
public static class WorkoutSuggestions
{
    public sealed record WorkoutInfo(
        Guid Id,
        string Name,
        IReadOnlyList<string> TypeNames,
        IReadOnlyList<string> Muscles,
        IReadOnlyList<string> SecondaryMuscles,
        string Category);

    public sealed record PastTraining(string Name, IReadOnlyCollection<Guid> WorkoutIds);

    private sealed record Focus(string[] Prefixes, string[] Muscles, string? Category = null);

    // Beginnings of words in names (lower case, without diacritics).
    private static readonly Focus[] Focuses =
    [
        new(["gornj", "upper"], [WorkoutMuscles.Chest, WorkoutMuscles.Shoulders, WorkoutMuscles.UpperBack,
            WorkoutMuscles.Lats, WorkoutMuscles.Biceps, WorkoutMuscles.Triceps]),
        new(["donj", "lower", "nog", "leg"], [WorkoutMuscles.Quads, WorkoutMuscles.Glutes, WorkoutMuscles.Hamstrings,
            WorkoutMuscles.Calves, WorkoutMuscles.Adductors]),
        new(["push", "potis"], [WorkoutMuscles.Chest, WorkoutMuscles.Shoulders, WorkoutMuscles.Triceps]),
        new(["pull", "vuc", "povlac"], [WorkoutMuscles.Lats, WorkoutMuscles.UpperBack, WorkoutMuscles.Biceps]),
        new(["prs", "chest"], [WorkoutMuscles.Chest]),
        new(["led", "back"], [WorkoutMuscles.Lats, WorkoutMuscles.UpperBack, WorkoutMuscles.LowerBack]),
        new(["ramen", "shoulder", "delt"], [WorkoutMuscles.Shoulders]),
        new(["ruk", "arm", "bicep", "tricep"], [WorkoutMuscles.Biceps, WorkoutMuscles.Triceps, WorkoutMuscles.Forearms]),
        new(["core", "trup", "trbu", "abs"], [WorkoutMuscles.Abs, WorkoutMuscles.Obliques, WorkoutMuscles.LowerBack]),
        new(["glut", "guz", "straznj"], [WorkoutMuscles.Glutes, WorkoutMuscles.Hamstrings]),
        new(["full", "cijel", "total", "whole"], [WorkoutMuscles.Chest, WorkoutMuscles.Shoulders, WorkoutMuscles.UpperBack,
            WorkoutMuscles.Lats, WorkoutMuscles.Abs, WorkoutMuscles.Glutes, WorkoutMuscles.Quads, WorkoutMuscles.Hamstrings]),
        new(["mobil", "istez", "stretch", "fleksib", "flexib"], [], WorkoutCategories.MobilityFlexibility),
        new(["snag", "strength", "kondicij", "condition"], [], WorkoutCategories.StrengthConditioning)
    ];

    // Words that say nothing about the exercises.
    private static readonly HashSet<string> StopWords =
    [
        "trening", "treninga", "training", "workout", "dan", "day", "session", "sesija", "the", "and", "for",
        "za", "sa", "dio", "dijela", "tijela", "body", "week", "tjedan", "plan", "program"
    ];

    public static List<Guid> Suggest(string trainingName, IReadOnlyList<WorkoutInfo> workouts,
        IEnumerable<PastTraining> pastTrainings, int limit = 12)
    {
        var words = Words(trainingName);
        if (words.Count == 0)
            return [];

        var scores = workouts.ToDictionary(x => x.Id, _ => 0.0);

        // Earlier trainings and templates with a similar name.
        foreach (var past in pastTrainings)
        {
            var similarity = Similarity(words, Words(past.Name));
            if (similarity < 0.5) continue;

            foreach (var workoutId in past.WorkoutIds.Distinct())
                if (scores.ContainsKey(workoutId))
                    scores[workoutId] = Math.Min(scores[workoutId] + 3 * similarity, 9);
        }

        var focuses = Focuses
            .Where(focus => words.Any(word => focus.Prefixes.Any(word.StartsWith)))
            .ToList();
        var focusMuscles = focuses.SelectMany(x => x.Muscles).ToHashSet();
        var focusCategories = focuses.Select(x => x.Category).OfType<string>().ToHashSet();

        foreach (var workout in workouts)
        {
            var muscleScore = workout.Muscles.Count(focusMuscles.Contains) * 2
                              + workout.SecondaryMuscles.Count(focusMuscles.Contains) * 0.5;
            scores[workout.Id] += Math.Min(muscleScore, 4);

            if (focusCategories.Contains(workout.Category))
                scores[workout.Id] += focusCategories.Count == 1 ? 3 : 1;

            // The same word in the workout's name or type.
            var workoutWords = Words(workout.Name);
            foreach (var typeName in workout.TypeNames)
                workoutWords.UnionWith(Words(typeName));
            scores[workout.Id] += Math.Min(words.Count(x => x.Length >= 4 && workoutWords.Contains(x)) * 2, 4);
        }

        var names = workouts.ToDictionary(x => x.Id, x => x.Name);
        return scores
            .Where(x => x.Value >= 1)
            .OrderByDescending(x => x.Value)
            .ThenBy(x => names[x.Key], StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.Key)
            .ToList();
    }

    // Shared words over all words of both names.
    private static double Similarity(HashSet<string> first, HashSet<string> second)
    {
        if (second.Count == 0) return 0;
        var shared = first.Count(second.Contains);
        return (double)shared / (first.Count + second.Count - shared);
    }

    private static HashSet<string> Words(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? []
            : Regex.Split(WorkoutMuscles.Normalize(text), @"[^\p{L}\p{N}]+")
                .Where(x => x.Length >= 3 && !StopWords.Contains(x))
                .ToHashSet();
}
