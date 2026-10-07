using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Authentication;

/// <summary>
/// Ends the session the request token belongs to.
/// </summary>
public sealed class LogoutEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;

    public LogoutEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/authentication/logout");
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionIdClaim = User.FindFirst(UserSessionSettings.SessionIdClaim)?.Value;

        if (Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            var now = DateTimeOffset.UtcNow;

            await _context.UserSessions
                .Where(x => x.Id == sessionId && x.RevokedAt == null)
                .ExecuteUpdateAsync(x => x
                        .SetProperty(session => session.RevokedAt, now)
                        .SetProperty(session => session.RevokeReason, SessionRevokeReasons.Logout)
                        .SetProperty(session => session.UpdatedAt, now),
                    cancellationToken);
        }

        await SendNoContentAsync(cancellationToken);
    }
}
