namespace ApexPerformance.API.Constants;

public static class PersonalRecordTypes
{
    /// <summary>Heaviest weight lifted in a set.</summary>
    public const string MaxWeight = "MaxWeight";

    /// <summary>Best estimated one rep max (Epley) of a set.</summary>
    public const string EstimatedOneRepMax = "EstimatedOneRepMax";

    /// <summary>Longest set, in seconds, of a workout done for a time (e.g. plank).</summary>
    public const string LongestDuration = "LongestDuration";

    /// <summary>Longest set, in metres, of a workout with a pace type (e.g. rowing).</summary>
    public const string LongestDistance = "LongestDistance";

    /// <summary>
    /// Fastest set of a workout with a pace type, as the average speed in metres per minute
    /// (higher is better like other records); the apps show it as the workout's pace.
    /// </summary>
    public const string FastestPace = "FastestPace";
}
