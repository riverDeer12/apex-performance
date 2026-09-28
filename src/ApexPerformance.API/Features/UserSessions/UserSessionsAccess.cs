using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.UserSessions;

public static class UserSessionsAccess
{
    /// <summary>
    /// Get ids of users whose sessions logged user can see.
    /// Super admin can see all users (returns null),
    /// coach can see only users of his clients.
    /// </summary>
    public static async Task<List<Guid>?> GetVisibleUserIds(ApexPerformanceContext context,
        ICurrentUserService currentUserService, CancellationToken cancellationToken)
    {
        if (currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin))
            return null;

        return await context.CoachClients
            .AsNoTracking()
            .Where(x => x.Coach.UserId == currentUserService.UserId)
            .Select(x => x.Client.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
