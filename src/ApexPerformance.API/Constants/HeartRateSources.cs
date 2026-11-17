namespace ApexPerformance.API.Constants;

/// <summary>
/// Where the client's phone read a training's heart rate: Apple Health on an
/// iPhone, Health Connect on Android (Mi Fitness, Google Fit, Fitbit, Samsung
/// Health... write into it).
/// </summary>
public static class HeartRateSources
{
    public const string AppleHealth = "apple_health";
    public const string HealthConnect = "health_connect";

    public static readonly IReadOnlyList<string> All = [AppleHealth, HealthConnect];

    /// <summary>
    /// Missing is allowed: iPhone builds before the source was added send none.
    /// </summary>
    public static bool IsValid(string? source) => source is null || All.Contains(source);

    /// <summary>
    /// The stored source; without one it is Apple Health, the only source before Android read heart rate.
    /// </summary>
    public static string Normalize(string? source) => source ?? AppleHealth;
}
