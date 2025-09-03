namespace ApexPerformance.API.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    
    /// <summary>
    /// Check if logged user has required role.
    /// </summary>
    /// <param name="requiredRole">Role that needs to be checked.</param>
    /// <returns></returns>
    bool LoggedUserHasRole(string requiredRole);
}