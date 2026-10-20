using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;

namespace ApexPerformance.Tests;

public class WorkoutMusclesTests
{
    private static WorkoutMuscles.Guessed Guess(string hr, string en = "")
        => WorkoutMuscles.Guess(new LocalizedProperty
        {
            Translations = { [Language.HR] = hr, [Language.EN] = en }
        }.ToJsonString());

    [Fact]
    public void Squat_works_legs()
    {
        var muscles = Guess("Stražnji čučanj", "Back squat");
        Assert.Equal([WorkoutMuscles.Glutes, WorkoutMuscles.Quads], muscles.Primary);
        Assert.Equal([WorkoutMuscles.Adductors], muscles.Secondary);
    }

    [Fact]
    public void Leg_curl_is_not_a_biceps_curl()
    {
        var muscles = Guess("Pregib nogu ležeći", "Lying leg curl");
        Assert.Contains(WorkoutMuscles.Hamstrings, muscles.Primary);
        Assert.DoesNotContain(WorkoutMuscles.Biceps, muscles.Primary.Concat(muscles.Secondary));
    }

    [Fact]
    public void Romanian_deadlift_is_not_a_deadlift()
    {
        var muscles = Guess("Rumunjsko mrtvo dizanje", "Romanian deadlift");
        Assert.Equal([WorkoutMuscles.Glutes, WorkoutMuscles.Hamstrings], muscles.Primary);
        Assert.Equal([WorkoutMuscles.LowerBack], muscles.Secondary);
    }

    [Fact]
    public void Bench_press_works_chest_helped_by_shoulders_and_triceps()
    {
        var muscles = Guess("Potisak s klupe", "Bench press");
        Assert.Equal([WorkoutMuscles.Chest], muscles.Primary);
        Assert.Equal([WorkoutMuscles.Shoulders, WorkoutMuscles.Triceps], muscles.Secondary);
    }

    [Fact]
    public void Primary_muscle_is_not_also_secondary()
    {
        // Bench press helps with triceps, dips work them.
        var muscles = Guess("Potisak s klupe i propadanja", "Bench press and dips");
        Assert.Contains(WorkoutMuscles.Triceps, muscles.Primary);
        Assert.DoesNotContain(WorkoutMuscles.Triceps, muscles.Secondary);
    }

    [Fact]
    public void Upright_row_is_not_a_row()
        => Assert.Equal([WorkoutMuscles.Shoulders], Guess("Veslanje u stojećem", "Upright row").Primary.Where(x =>
            x != WorkoutMuscles.UpperBack).ToList());

    [Fact]
    public void Unknown_workout_has_no_muscles()
    {
        var muscles = Guess("Istezanje", "Stretching");
        Assert.Empty(muscles.Primary);
        Assert.Empty(muscles.Secondary);
    }

    [Fact]
    public void Serialize_keeps_known_muscles_in_order()
        => Assert.Equal("chest,abs", WorkoutMuscles.Serialize(["abs", "chest", "unknown", "abs"]));

    [Fact]
    public void Apply_keeps_secondary_muscles_out_of_primary()
    {
        var workout = new Workout { Muscles = "chest", SecondaryMuscles = "shoulders,triceps" };

        WorkoutMuscles.Apply(workout, ["chest", "triceps"], null);

        Assert.Equal("chest,triceps", workout.Muscles);
        Assert.Equal("shoulders", workout.SecondaryMuscles);
    }
}
