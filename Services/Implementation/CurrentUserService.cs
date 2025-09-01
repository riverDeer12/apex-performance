using ApexPerformance.API.Constants;

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

    public bool UserIsClient
    {
        get
        {
            var roleValues = _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

            return roleValues?.Contains(UserRoles.Client) ?? false; ;
        }
    }
}