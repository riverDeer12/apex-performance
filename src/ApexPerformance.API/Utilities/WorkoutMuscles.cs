using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Utilities.Localization;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Utilities;

/// <summary>
/// Muscles a workout works, shown highlighted on a body map in the apps.
/// Stored on the workout as comma separated codes, primary ones (doing most
/// of the work) and secondary ones (helping); null means they were never
/// set, then they are guessed from the workout's name and the coach can
/// correct them.
/// </summary>
public static class WorkoutMuscles
{
    public const string Chest = "chest";
    public const string Shoulders = "shoulders";
    public const string Biceps = "biceps";
    public const string Triceps = "triceps";
    public const string Forearms = "forearms";
    public const string Abs = "abs";
    public const string Obliques = "obliques";
    public const string Traps = "traps";
    public const string UpperBack = "upper_back";
    public const string Lats = "lats";
    public const string LowerBack = "lower_back";
    public const string Glutes = "glutes";
    public const string Quads = "quads";
    public const string Hamstrings = "hamstrings";
    public const string Adductors = "adductors";
    public const string Calves = "calves";

    public static readonly IReadOnlyList<string> All =
    [
        Chest, Shoulders, Biceps, Triceps, Forearms, Abs, Obliques, Traps,
        UpperBack, Lats, LowerBack, Glutes, Quads, Hamstrings, Adductors, Calves
    ];

    public static bool IsValid(string muscle) => All.Contains(muscle);

    public static List<string> Parse(string? stored)
        => string.IsNullOrWhiteSpace(stored)
            ? []
            : stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsValid)
                .ToList();

    // In the order of All, without duplicates.
    public static string Serialize(IEnumerable<string> muscles)
    {
        var set = muscles.ToHashSet();
        return string.Join(',', All.Where(set.Contains));
    }

    private sealed record Rule(string Pattern, string[] Primary, string[] Secondary, string? Unless = null);

    // Patterns run on the Croatian and English names together, lower case
    // without diacritics. More specific rules come with an "unless" pattern
    // so e.g. a leg curl isn't taken for a biceps curl. Primary muscles do
    // most of the work, secondary ones help.
    private static readonly Rule[] Rules =
    [
        new(@"bench|potisak s klupe|potisak na klupi|chest press|push ?-?ups?\b|sklekov|sklek", [Chest], [Triceps, Shoulders]),
        new(@"\bfly\b|\bflyes?\b|razvlacenj|crossover|pec deck|\bprsa\b|\bchest\b", [Chest], [Shoulders], Unless: @"reverse|rear|obrnut"),
        new(@"\bdips?\b|propadanj", [Chest, Triceps], []),
        new(@"overhead press|shoulder press|military|potisak iznad glave|vojnick|arnold|\bohp\b|landmine press", [Shoulders], [Triceps]),
        new(@"lateral raise|front raise|odrucenj|predrucenj|upright row|\bdeltoid", [Shoulders], []),
        new(@"rear delt|face ?pull|reverse fly|obrnuto razvlacenj", [Shoulders], [UpperBack]),
        new(@"shrug|slijeganj", [Traps], []),
        new(@"pull ?-?ups?\b|chin ?-?ups?\b|zgib|pulldown|lat pull|povlacenj", [Lats], [Biceps, UpperBack]),
        new(@"\brows?\b|rowing|veslanj", [UpperBack, Lats], [Biceps], Unless: @"upright"),
        new(@"romanian|\brdl\b|rumunjsk|stiff ?-?leg|good ?morning", [Hamstrings, Glutes], [LowerBack]),
        new(@"deadlift|mrtvo dizanj|mrtvog dizanj", [Hamstrings, Glutes, LowerBack], [Traps], Unless: @"romanian|\brdl\b|rumunjsk|stiff"),
        new(@"back extension|hyperextension|hiperekstenzij|superman|ekstenzija leda", [LowerBack], [Glutes]),
        new(@"squat|cucanj|cucnj|cucanja", [Quads, Glutes], [Adductors]),
        new(@"leg press|nozni potisak|potisak nogama", [Quads], [Glutes]),
        new(@"lunge|iskorak|split squat|step ?-?ups?\b|bugarsk|penjanje na klupu", [Quads, Glutes], []),
        new(@"leg extension|ekstenzija nog|opruzanje nog|ekstenzije nog", [Quads], []),
        new(@"leg curl|hamstring curl|pregib nog|pregibi nog|nordic|nordijsk", [Hamstrings], []),
        new(@"hip thrust|glute bridge|hip bridge|potisak kuk|\bmost\b|\bglute", [Glutes], [Hamstrings]),
        new(@"abduct|abdukc", [Glutes], []),
        new(@"adduct|adukc|copenhagen", [Adductors], []),
        new(@"\bcalf|calves|listov|listova|podizanje na prste|na prste", [Calves], []),
        new(@"\bcurls?\b|pregib|biceps|bicep", [Biceps], [],
            Unless: @"leg curl|hamstring|pregib nog|pregibi nog|nordic|wrist|zapesc|pregib trupa|trbus"),
        new(@"hammer|cekic", [Biceps], [Forearms]),
        new(@"triceps|tricep|skull ?crusher|pushdown|push ?-?down|french press|francusk|kickback", [Triceps], [],
            Unless: @"glute|kuk"),
        new(@"wrist|zapesc|forearm|podlaktic|farmer|grip|stisak", [Forearms], []),
        new(@"farmer", [], [Traps]),
        new(@"plank|crunch|trbusnj|trbusn|sit ?-?ups?\b|hollow|dead ?bug|leg raise|podizanje nogu|ab wheel|rollout|pregib trupa|\babs?\b", [Abs], []),
        new(@"russian twist|side plank|bocni|pallof|woodchop|wood chop|oblique|kosi trbus|rotacij|twist", [Obliques], [Abs]),
        new(@"burpee|thruster|power clean|hang clean|\bclean\b|snatch|kettlebell swing|\bswings?\b|zamah", [Glutes, Hamstrings, Quads], [Shoulders])
    ];

    public sealed record Guessed(List<string> Primary, List<string> Secondary);

    /// <summary>
    /// Muscles guessed from the workout's names (stored JSON of translations).
    /// A muscle primary in one matching rule isn't secondary.
    /// </summary>
    public static Guessed Guess(string localizedNameJson)
    {
        var name = new LocalizedProperty(localizedNameJson);
        var text = Normalize($"{name.Get(Language.HR)} {name.Get(Language.EN)}");
        var primary = new HashSet<string>();
        var secondary = new HashSet<string>();

        foreach (var rule in Rules)
        {
            if (!Regex.IsMatch(text, rule.Pattern)) continue;
            if (rule.Unless is not null && Regex.IsMatch(text, rule.Unless)) continue;
            primary.UnionWith(rule.Primary);
            secondary.UnionWith(rule.Secondary);
        }

        return new Guessed(
            All.Where(primary.Contains).ToList(),
            All.Where(x => secondary.Contains(x) && !primary.Contains(x)).ToList());
    }

    /// <summary>
    /// Sets the muscles given, null ones stay. A muscle can't be both,
    /// primary wins.
    /// </summary>
    public static void Apply(Workout workout, IEnumerable<string>? primary, IEnumerable<string>? secondary)
    {
        if (primary is not null)
            workout.Muscles = Serialize(primary);

        var primaryMuscles = Parse(workout.Muscles);
        workout.SecondaryMuscles = Serialize(
            (secondary ?? Parse(workout.SecondaryMuscles)).Where(x => !primaryMuscles.Contains(x)));
    }

    public static void Apply(Workout workout, Guessed guessed)
        => Apply(workout, guessed.Primary, guessed.Secondary);

    public static List<string> ParseSecondary(Workout workout)
    {
        var primary = Parse(workout.Muscles);
        return Parse(workout.SecondaryMuscles).Where(x => !primary.Contains(x)).ToList();
    }

    private static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString();
    }
}

/// <summary>
/// Guesses the muscles of workouts that never had them set. Runs at start-up.
/// </summary>
public class WorkoutMusclesJob
{
    private readonly ApexPerformanceContext _context;

    public WorkoutMusclesJob(ApexPerformanceContext context)
    {
        _context = context;
    }

    public async Task FillMissingAsync()
    {
        var workouts = await _context.Workouts
            .Where(x => x.Muscles == null || x.SecondaryMuscles == null)
            .ToListAsync();

        if (workouts.Count == 0) return;

        foreach (var workout in workouts)
        {
            var guessed = WorkoutMuscles.Guess(workout.Name);

            // Guessed before secondary muscles existed, all as primary: split
            // them now. Ones the coach changed are kept as they are.
            var isOldGuess = workout.Muscles is not null &&
                             WorkoutMuscles.Parse(workout.Muscles).ToHashSet()
                                 .SetEquals(guessed.Primary.Concat(guessed.Secondary));

            if (workout.Muscles is null || isOldGuess)
                WorkoutMuscles.Apply(workout, guessed);
            else
                workout.SecondaryMuscles = string.Empty;
        }

        await _context.SaveChangesAsync();
    }
}
