using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.UserSessions;

public sealed record GetUserLastSessionResponse(
    Guid UserId,
    string Username,
    string? FullName,
    string Email,
    List<string> Roles,
    DateTimeOffset? LastLoginAt,
    string? LastIpAddress,
    string? LastUserAgent,
    int LoginsCount,
    Guid? ActiveSessionId);

/// <summary>
/// Users with their last login. Super admin sees
/// all users, coach sees only his clients.
/// </summary>
public sealed class GetUserSessionsEndpoint : EndpointWithoutRequest<List<GetUserLastSessionResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserSessionsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/user-sessions");
        Roles(UserRoles.SuperAdmin, UserRoles.Coach);
        Options(x => x.WithTags("UserSessions"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userIds = await UserSessionsAccess.GetVisibleUserIds(_context, _currentUserService, cancellationToken);

        var usersQuery = _context.Users.AsNoTracking();

        if (userIds is not null)
            usersQuery = usersQuery.Where(x => userIds.Contains(x.Id));

        var users = await usersQuery
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.Email,
                Roles = x.Roles.Select(userRole => userRole.Role.Name).ToList(),
                ClientName = x.Client != null ? x.Client.FirstName + " " + x.Client.LastName : null,
                CoachName = x.Coach != null ? x.Coach.FirstName + " " + x.Coach.LastName : null,
                AdministratorName = x.Administrator != null
                    ? x.Administrator.FirstName + " " + x.Administrator.LastName
                    : null
            })
            .ToListAsync(cancellationToken);

        var visibleUserIds = users.Select(x => x.Id).ToList();

        var loginsCounts = await _context.UserSessions
            .AsNoTracking()
            .Where(x => visibleUserIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var lastSessions = await _context.UserSessions
            .AsNoTracking()
            .Where(x => visibleUserIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => x.OrderByDescending(session => session.CreatedAt)
                .Select(session => new { session.UserId, session.CreatedAt, session.IpAddress, session.UserAgent })
                .First())
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        // User can have only one active session (one device at a time).
        var activeSessions = await _context.UserSessions
            .AsNoTracking()
            .Where(x => visibleUserIds.Contains(x.UserId) &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, SessionId = x.OrderByDescending(session => session.CreatedAt).First().Id })
            .ToDictionaryAsync(x => x.UserId, x => x.SessionId, cancellationToken);

        var response = users.Select(x =>
            {
                lastSessions.TryGetValue(x.Id, out var lastSession);
                loginsCounts.TryGetValue(x.Id, out var loginsCount);
                Guid? activeSessionId = activeSessions.TryGetValue(x.Id, out var sessionId) ? sessionId : null;

                return new GetUserLastSessionResponse(
                    x.Id,
                    x.UserName,
                    x.ClientName ?? x.CoachName ?? x.AdministratorName,
                    x.Email,
                    x.Roles,
                    lastSession?.CreatedAt,
                    lastSession?.IpAddress,
                    lastSession?.UserAgent,
                    loginsCount,
                    activeSessionId);
            })
            .OrderByDescending(x => x.LastLoginAt.HasValue)
            .ThenByDescending(x => x.LastLoginAt)
            .ToList();

        await SendAsync(response, cancellation: cancellationToken);
    }
}
