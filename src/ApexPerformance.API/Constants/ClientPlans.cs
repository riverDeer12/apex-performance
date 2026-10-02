namespace ApexPerformance.API.Constants;

/// <summary>
/// How the client trains: individual trainings with a coach,
/// online with occasional consultations, or a membership
/// with prepared training plans.
/// </summary>
public static class ClientPlans
{
    public const string PrivateCoaching = nameof(PrivateCoaching);
    public const string OnlineCoaching = nameof(OnlineCoaching);
    public const string Membership = nameof(Membership);

    public static readonly string[] All = [PrivateCoaching, OnlineCoaching, Membership];
}
