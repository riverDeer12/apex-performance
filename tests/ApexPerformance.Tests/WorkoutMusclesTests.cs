using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;

namespace ApexPerformance.Tests;

public class WorkoutMusclesTests
{
    private static List<string> Guess(string hr, string en = "")
        => WorkoutMuscles.Guess(new LocalizedProperty
        {
            Translations = { [Language.HR] = hr, [Language.EN] = en }
        }.ToJsonString());

    [Fact]
    public void Squat_works_legs()
        => Assert.Equal([WorkoutMuscles.Glutes, WorkoutMuscles.Quads, WorkoutMuscles.Adductors],
            Guess("Stražnji čučanj", "Back squat"));

    [Fact]
    public void Leg_curl_is_not_a_biceps_curl()
    {
        var muscles = Guess("Pregib nogu ležeći", "Lying leg curl");
        Assert.Contains(WorkoutMuscles.Hamstrings, muscles);
        Assert.DoesNotContain(WorkoutMuscles.Biceps, muscles);
    }

    [Fact]
    public void Romanian_deadlift_is_not_a_deadlift()
    {
        var muscles = Guess("Rumunjsko mrtvo dizanje", "Romanian deadlift");
        Assert.Equal([WorkoutMuscles.LowerBack, WorkoutMuscles.Glutes, WorkoutMuscles.Hamstrings], muscles);
    }

    [Fact]
    public void Bench_press_works_chest()
        => Assert.Equal([WorkoutMuscles.Chest, WorkoutMuscles.Shoulders, WorkoutMuscles.Triceps],
            Guess("Potisak s klupe", "Bench press"));

    [Fact]
    public void Upright_row_is_not_a_row()
        => Assert.Equal([WorkoutMuscles.Shoulders], Guess("Veslanje u stojećem", "Upright row").Where(x =>
            x != WorkoutMuscles.UpperBack).ToList());

    [Fact]
    public void Unknown_workout_has_no_muscles()
        => Assert.Empty(Guess("Istezanje", "Stretching"));

    [Fact]
    public void Serialize_keeps_known_muscles_in_order()
        => Assert.Equal("chest,abs", WorkoutMuscles.Serialize(["abs", "chest", "unknown", "abs"]));
}
