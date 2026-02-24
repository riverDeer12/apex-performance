using ApexPerformance.API.Constants;
using ApexPerformance.API.Services.Interfaces;

namespace ApexPerformance.API.Services.Implementation;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            return userIdClaim is null ? Guid.Empty : Guid.Parse(userIdClaim);
        }
    }

    public bool LoggedUserHasRole(string requiredRole)
    {
        var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
        
        if (string.IsNullOrWhiteSpace(roleClaim)) return false;
        
        var roles = roleClaim.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        
        return roles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase);
    }
}