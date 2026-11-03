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

    private static WorkoutMuscles.Guessed GuessFullBody(string hr, string en = "")
        => WorkoutMuscles.Guess(new LocalizedProperty
        {
            Translations = { [Language.HR] = hr, [Language.EN] = en }
        }.ToJsonString(), isFullBody: true);

    [Fact]
    public void Full_body_burpee_works_upper_and_lower_body()
    {
        var muscles = GuessFullBody("Burpee", "Burpee");
        Assert.Contains(WorkoutMuscles.Chest, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Quads, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Shoulders, muscles.Secondary);
        Assert.Contains(WorkoutMuscles.Abs, muscles.Secondary);
    }

    [Fact]
    public void Full_body_rules_are_used_only_for_full_body_workouts()
    {
        Assert.DoesNotContain(WorkoutMuscles.Chest, Guess("Burpee").Primary);
        Assert.Empty(Guess("Mountain climber").Primary);
        Assert.Contains(WorkoutMuscles.Abs, GuessFullBody("Penjač", "Mountain climber").Primary);
    }

    [Fact]
    public void Unknown_full_body_exercise_works_the_big_muscles()
    {
        var muscles = GuessFullBody("Kompleks s bučicama", "Dumbbell complex");
        Assert.Contains(WorkoutMuscles.Chest, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Quads, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Lats, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Calves, muscles.Secondary);
    }

    [Fact]
    public void Full_body_guess_keeps_the_general_rules()
    {
        var muscles = GuessFullBody("Čučanj s potiskom", "Thruster");
        Assert.Contains(WorkoutMuscles.Shoulders, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Quads, muscles.Primary);
        Assert.Contains(WorkoutMuscles.Glutes, muscles.Primary);
    }

    [Theory]
    [InlineData("Full body", "Full body", true)]
    [InlineData("Cijelo tijelo", "Whole body", true)]
    [InlineData("Vježbe za cijelo tijelo", "", true)]
    [InlineData("Snaga", "Strength", false)]
    [InlineData("Gornji dio tijela", "Upper body", false)]
    public void Full_body_type_by_name(string hr, string en, bool isFullBody)
        => Assert.Equal(isFullBody, WorkoutMuscles.IsFullBodyType(new LocalizedProperty
        {
            Translations = { [Language.HR] = hr, [Language.EN] = en }
        }.ToJsonString()));
}
