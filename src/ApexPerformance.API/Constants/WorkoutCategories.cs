namespace ApexPerformance.API.Constants;

/// <summary>
/// Category of a workout, for filtering the workout list: mobility and
/// flexibility workouts, all others are strength and conditioning.
/// </summary>
public static class WorkoutCategories
{
    public const string MobilityFlexibility = nameof(MobilityFlexibility);
    public const string StrengthConditioning = nameof(StrengthConditioning);

    public static readonly string[] All = [MobilityFlexibility, StrengthConditioning];

    public static bool IsValid(string? category) => category is not null && All.Contains(category);
}
