using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.UserSessions;

public sealed record GetUserSessionResponse(
    Guid Id,
    DateTimeOffset LoggedInAt,
    string? IpAddress,
    string? UserAgent,
    bool RememberMe,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevokeReason,
    bool IsActive);

/// <summary>
/// Latest logins of one user. Coach
/// can see only logins of his clients.
/// </summary>
public sealed class GetUserSessionHistoryEndpoint : EndpointWithoutRequest<List<GetUserSessionResponse>>
{
    private const int MaxSessions = 100;

    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserSessionHistoryEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/user-sessions/{userId}");
        Roles(UserRoles.SuperAdmin, UserRoles.Coach);
        Options(x => x.WithTags("UserSessions"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = Route<Guid>("userId", isRequired: true);

        var visibleUserIds =
            await UserSessionsAccess.GetVisibleUserIds(_context, _currentUserService, cancellationToken);

        if (visibleUserIds is not null && !visibleUserIds.Contains(userId))
            ThrowError(ErrorCodes.UnauthorizedAction, StatusCodes.Status403Forbidden);

        var now = DateTimeOffset.UtcNow;

        var sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxSessions)
            .Select(x => new GetUserSessionResponse(x.Id, x.CreatedAt, x.IpAddress, x.UserAgent, x.RememberMe,
                x.ExpiresAt, x.RevokedAt, x.RevokeReason,
                x.RevokedAt == null && x.ExpiresAt != null && x.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        await SendAsync(sessions, cancellation: cancellationToken);
    }
}
