using System.Linq.Expressions;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
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
            ThrowError(ErrorMessages.NotFound);

        if (!user.IsValidPassword(request.Password))
            ThrowError(ValidationMessages.WrongUserNameOrPassword);

        var jwtToken = await _authenticationService.GenerateJwtToken(request.RememberMe, user);

        await SendAsync(new LoginResponse(jwtToken), cancellation: cancellationToken);
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