using System.Text.Json;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Workouts;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities.Localization;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.Tests;

public class WorkoutFavoritesTests
{
    private static readonly Guid Squat = Guid.Parse("0f9e4c1a-2b3d-4e5f-8a6b-7c8d9e0f1a2b");
    private static readonly Guid Plank = Guid.Parse("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d");

    private static GetWorkoutResponse Workout(Guid id)
        => new(id, new LocalizedProperty("{\"hr\":\"Čučanj\",\"en\":\"Squat\"}"),
            new LocalizedProperty("{}"), "", "", [], [], [], false, WorkoutCategories.StrengthConditioning);

    [Fact]
    public void Not_a_favorite_unless_marked()
        => Assert.False(Workout(Squat).IsFavorite);

    [Fact]
    public void Mark_sets_only_the_users_favorites()
    {
        var marked = WorkoutFavorites.Mark([Workout(Squat), Workout(Plank)], new HashSet<Guid> { Plank });

        Assert.False(marked.Single(x => x.Id == Squat).IsFavorite);
        Assert.True(marked.Single(x => x.Id == Plank).IsFavorite);
        // Order is kept: favorites are put first by the apps where they need it.
        Assert.Equal([Squat, Plank], marked.Select(x => x.Id));
    }

    [Fact]
    public void Mark_with_no_favorites_marks_none()
        => Assert.All(WorkoutFavorites.Mark([Workout(Squat), Workout(Plank)], new HashSet<Guid>()),
            x => Assert.False(x.IsFavorite));

    [Fact]
    public void Response_has_isFavorite_next_to_the_old_fields()
    {
        var json = JsonSerializer.Serialize(Workout(Squat) with { IsFavorite = true },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("isFavorite").GetBoolean());
        // Older apps keep reading the fields they know.
        Assert.Equal(Squat, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(WorkoutCategories.StrengthConditioning,
            document.RootElement.GetProperty("category").GetString());
        Assert.False(document.RootElement.GetProperty("isTimed").GetBoolean());
    }

    [Fact]
    public void Model_has_one_favorite_per_user_and_workout_and_hides_deleted_workouts()
    {
        using var context = new ApexPerformanceContext(new DbContextOptionsBuilder<ApexPerformanceContext>()
            .UseSqlServer("Server=unused;Database=unused").Options, new NoUser());
        var entity = context.Model.FindEntityType(typeof(FavoriteWorkout))!;

        Assert.Equal("FavoriteWorkouts", entity.GetTableName());
        var index = Assert.Single(entity.GetIndexes(), x => x.IsUnique);
        Assert.Equal(["UserId", "WorkoutId"], index.Properties.Select(x => x.Name));
        Assert.All(entity.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.Cascade, x.DeleteBehavior));
        Assert.NotNull(entity.GetQueryFilter());
    }

    private sealed class NoUser : ICurrentUserService
    {
        public Guid UserId => Guid.Empty;
        public bool LoggedUserHasRole(string requiredRole) => false;
    }
}
