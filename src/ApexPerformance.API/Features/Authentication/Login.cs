using System.Linq.Expressions;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Authentication;

public sealed record LoginRequest(string Username, string Password, bool RememberMe);

public sealed record LoginResponse(string Token);

public sealed class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAuthenticationService _authenticationService;

    public LoginEndpoint(ApexPerformanceContext context, IConfiguration configuration,
        IAuthenticationService authenticationService)
    {
        _context = context;
        _authenticationService = authenticationService;
    }

    public override void Configure()
    {
        Post("api/authentication/login");
        AllowAnonymous();
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(x => x.Roles).ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(x => x.UserName == request.Username, cancellationToken);

        if (user is null)
            ThrowError(ErrorCodes.UserNotFound);

        if (!user.IsValidPassword(request.Password))
            ThrowError(ErrorCodes.WrongUserNameOrPassword);

        var expiresAt = _authenticationService.GetTokenExpiration(request.RememberMe);

        var session = await StartUserSession(user.Id, request.RememberMe, expiresAt, cancellationToken);

        var jwtToken = await _authenticationService.GenerateJwtToken(user, session.Id, expiresAt);

        await SendAsync(new LoginResponse(jwtToken), cancellation: cancellationToken);
    }

    /// <summary>
    /// User can be logged in on only one device at a time,
    /// so logging in ends all other active sessions of the user
    /// and the new session is the only one its token is valid for.
    /// </summary>
    private async Task<UserSession> StartUserSession(Guid userId, bool rememberMe, DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await _context.UserSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now)
            .ExecuteUpdateAsync(x => x
                    .SetProperty(session => session.RevokedAt, now)
                    .SetProperty(session => session.RevokeReason, SessionRevokeReasons.NewLogin)
                    .SetProperty(session => session.UpdatedAt, now)
                    .SetProperty(session => session.UpdatedBy, userId),
                cancellationToken);

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var session = new UserSession
        {
            UserId = userId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent.Length > 512 ? userAgent[..512] : userAgent,
            RememberMe = rememberMe,
            ExpiresAt = expiresAt,
            // Login is anonymous request, so audit
            // fields are set to user that logged in.
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.UserSessions.Add(session);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        return session;
    }
}

public sealed class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}