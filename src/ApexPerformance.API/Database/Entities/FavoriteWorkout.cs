using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Workout a user (coach or client) marked with the star, so it is easy to find
/// in the workouts list and first in the exercise picker of the training form.
/// Every user has their own favorites.
/// </summary>
public class FavoriteWorkout
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
