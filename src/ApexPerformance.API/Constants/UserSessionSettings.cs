namespace ApexPerformance.API.Constants;

public static class UserSessionSettings
{
    /// <summary>
    /// Claim in login token with id of the user session
    /// it belongs to, so revoked sessions can be rejected.
    /// </summary>
    public const string SessionIdClaim = "session_id";
}

public static class SessionRevokeReasons
{
    /// <summary>User logged in on another device.</summary>
    public const string NewLogin = "NewLogin";

    /// <summary>Session revoked through administration.</summary>
    public const string Administrator = "Administrator";

    /// <summary>User logged out.</summary>
    public const string Logout = "Logout";
}
