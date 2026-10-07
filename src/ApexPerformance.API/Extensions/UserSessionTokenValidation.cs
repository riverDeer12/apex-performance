using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Extensions;

public static class UserSessionTokenValidation
{
    /// <summary>
    /// Rejects login tokens whose user session was revoked
    /// (logged in on another device, revoked by administrator
    /// or logged out). Login tokens are recognized by roles,
    /// tokens without roles (set password links, jobs dashboard)
    /// are not bound to a session and are left as they are.
    /// </summary>
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (principal is null || !principal.HasClaim(x => x.Type == "role"))
            return;

        var sessionIdClaim = principal.FindFirst(UserSessionSettings.SessionIdClaim)?.Value;
        var userIdClaim = principal.FindFirst("sub")?.Value;

        // Login tokens issued before sessions were bound to tokens.
        if (!Guid.TryParse(sessionIdClaim, out var sessionId) || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail("Login session is not valid.");
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApexPerformanceContext>();

        var now = DateTimeOffset.UtcNow;

        var isActive = await dbContext.UserSessions
            .AsNoTracking()
            .AnyAsync(x => x.Id == sessionId &&
                           x.UserId == userId &&
                           x.RevokedAt == null &&
                           x.ExpiresAt > now,
                context.HttpContext.RequestAborted);

        if (!isActive)
            context.Fail("Login session has ended.");
    }
}
