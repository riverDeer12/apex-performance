using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.UserSessions;

/// <summary>
/// Super admin ends a session of any user, the device
/// using it is logged out on its next request.
/// </summary>
public sealed class RevokeUserSessionEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RevokeUserSessionEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/user-sessions/{sessionId}/revoke");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("UserSessions"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionId = Route<Guid>("sessionId", isRequired: true);

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.RevokeReason = SessionRevokeReasons.Administrator;
            session.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);
        }

        await SendAsync(new StatusResponse(session.Id, true), cancellation: cancellationToken);
    }
}
