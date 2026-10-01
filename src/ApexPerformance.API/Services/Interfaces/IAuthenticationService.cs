using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services.Interfaces;

public interface IAuthenticationService
{
    Task<string> GenerateJwtToken(bool rememberMe, User user);

    /// <summary>
    /// Token without roles and permissions that only
    /// identifies the user on the set password call.
    /// </summary>
    string GenerateSetPasswordToken(User user, TimeSpan validFor);
}