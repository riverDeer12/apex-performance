namespace ApexPerformance.API.Constants;

/// <summary>
/// How the pace of a workout done over a distance is shown. A workout
/// without one doesn't record distance.
/// </summary>
public static class WorkoutPaceTypes
{
    /// <summary>Minutes per kilometre, e.g. walking or running.</summary>
    public const string PerKilometre = "PerKm";

    /// <summary>Minutes per 500 metres, as rowing and ski ergometers show it.</summary>
    public const string Per500Metres = "Per500m";

    /// <summary>Kilometres per hour, e.g. cycling or an air bike.</summary>
    public const string KilometresPerHour = "Kmh";

    public static readonly string[] All = [PerKilometre, Per500Metres, KilometresPerHour];
}
