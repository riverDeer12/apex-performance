using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services.Interfaces;

public interface IAuthenticationService
{
    /// <summary>
    /// Login token bound to the given user session.
    /// </summary>
    Task<string> GenerateJwtToken(User user, Guid sessionId, DateTime expiresAt);

    DateTime GetTokenExpiration(bool rememberMe);

    /// <summary>
    /// Token without roles and permissions that only
    /// identifies the user on the set password call.
    /// </summary>
    string GenerateSetPasswordToken(User user, TimeSpan validFor);
}