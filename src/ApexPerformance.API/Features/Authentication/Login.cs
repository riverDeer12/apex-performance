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

        var jwtToken = await _authenticationService.GenerateJwtToken(request.RememberMe, user);

        await SaveUserSession(user.Id, request.RememberMe, cancellationToken);

        await SendAsync(new LoginResponse(jwtToken), cancellation: cancellationToken);
    }

    /// <summary>
    /// Record successful login so admins and coaches
    /// can see when user last logged in. Failing to save
    /// session must not prevent user from logging in.
    /// </summary>
    private async Task SaveUserSession(Guid userId, bool rememberMe, CancellationToken cancellationToken)
    {
        try
        {
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _context.UserSessions.Add(new UserSession
            {
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = userAgent.Length > 512 ? userAgent[..512] : userAgent,
                RememberMe = rememberMe,
                // Login is anonymous request, so audit
                // fields are set to user that logged in.
                CreatedBy = userId,
                UpdatedBy = userId
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Saving user session failed: {ex.Message}");
        }
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