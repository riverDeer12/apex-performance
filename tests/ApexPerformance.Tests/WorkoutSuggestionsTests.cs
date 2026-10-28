using ApexPerformance.API.Constants;
using ApexPerformance.API.Utilities;

namespace ApexPerformance.Tests;

public class WorkoutSuggestionsTests
{
    private static readonly Guid Bench = Guid.NewGuid();
    private static readonly Guid Squat = Guid.NewGuid();
    private static readonly Guid Row = Guid.NewGuid();
    private static readonly Guid Stretch = Guid.NewGuid();
    private static readonly Guid Plank = Guid.NewGuid();

    private static readonly List<WorkoutSuggestions.WorkoutInfo> Workouts =
    [
        new(Bench, "Potisak s klupe Bench press", ["Snaga"], [WorkoutMuscles.Chest],
            [WorkoutMuscles.Triceps, WorkoutMuscles.Shoulders], WorkoutCategories.StrengthConditioning),
        new(Squat, "Čučanj Back squat", ["Snaga"], [WorkoutMuscles.Quads, WorkoutMuscles.Glutes],
            [WorkoutMuscles.Adductors], WorkoutCategories.StrengthConditioning),
        new(Row, "Veslanje Barbell row", ["Snaga"], [WorkoutMuscles.UpperBack, WorkoutMuscles.Lats],
            [WorkoutMuscles.Biceps], WorkoutCategories.StrengthConditioning),
        new(Stretch, "Istezanje pregibača kuka Hip flexor stretch", ["Mobilnost i istezanje"], [WorkoutMuscles.Quads],
            [], WorkoutCategories.MobilityFlexibility),
        new(Plank, "Plank", ["Trup"], [WorkoutMuscles.Abs], [WorkoutMuscles.Obliques],
            WorkoutCategories.StrengthConditioning)
    ];

    [Fact]
    public void Upper_body_suggests_upper_body_workouts()
    {
        var suggested = WorkoutSuggestions.Suggest("Gornji dio tijela", Workouts, []);
        Assert.Contains(Bench, suggested);
        Assert.Contains(Row, suggested);
        Assert.DoesNotContain(Squat, suggested);
    }

    [Fact]
    public void Leg_day_suggests_squat_first()
    {
        var suggested = WorkoutSuggestions.Suggest("Leg day", Workouts, []);
        Assert.Equal(Squat, suggested.First());
        Assert.DoesNotContain(Bench, suggested);
    }

    [Fact]
    public void Mobility_suggests_mobility_workouts()
    {
        var suggested = WorkoutSuggestions.Suggest("Mobilnost", Workouts, []);
        Assert.Equal([Stretch], suggested);
    }

    [Fact]
    public void Earlier_trainings_with_a_similar_name_come_first()
    {
        var suggested = WorkoutSuggestions.Suggest("Ponedjeljak A", Workouts,
            [new WorkoutSuggestions.PastTraining("Ponedjeljak A", [Plank, Squat])]);
        Assert.Equal(2, suggested.Count);
        Assert.Contains(Plank, suggested);
        Assert.Contains(Squat, suggested);
    }

    [Fact]
    public void Word_of_a_workout_name_is_suggested()
    {
        var suggested = WorkoutSuggestions.Suggest("Squat focus", Workouts, []);
        Assert.Equal(Squat, suggested.First());
    }

    [Fact]
    public void Nothing_for_an_empty_or_unknown_name()
    {
        Assert.Empty(WorkoutSuggestions.Suggest("", Workouts, []));
        Assert.Empty(WorkoutSuggestions.Suggest("Trening", Workouts, []));
    }
}
