using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Users;

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password
);

public sealed record RegisterUserResponse(
    Guid Id,
    string Username,
    string Token
);

public sealed class RegisterUserEndpoint : Endpoint<RegisterUserRequest, RegisterUserResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IAuthenticationService _authenticationService;

    public RegisterUserEndpoint(ApexPerformanceContext context, IUserService userService,
        IEmailService emailService, IAuthenticationService authenticationService)
    {
        _context = context;
        _userService = userService;
        _emailService = emailService;
        _authenticationService = authenticationService;
    }

    public override void Configure()
    {
        Post("api/users/register");
        AllowAnonymous();
        Options(x => x.WithTags("Users"));
    }

    public override async Task HandleAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var clientRole = await _context.Roles
            .SingleAsync(x => x.Name == UserRoles.Client, cancellationToken);

        var user = await _userService.CreateUserAccount(request.Email, request.Password, request.Email,
            clientRole, cancellationToken);

        var client = new Client
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            User = user
        };

        _context.Clients.Add(client);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        var jwtToken = await _authenticationService.GenerateJwtToken(false, user);

        BackgroundJob.Enqueue(() => SendCredentialsEmail(user.Id, request.Password, jwtToken));

        await SendAsync(new RegisterUserResponse(user.Id, user.UserName, jwtToken),
            cancellation: cancellationToken);
    }

    public async Task SendCredentialsEmail(Guid userId, string password, string jwtToken)
    {
        var user = await _context.Users.SingleAsync(x => x.Id == userId);

        _emailService.SendCredentialsEmail(user, password, jwtToken);
    }
}

public sealed class RegisterUserValidator : Validator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ErrorCodes.Required)
            .EmailAddress().WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Phone).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Password).NotEmpty().WithMessage(ErrorCodes.Required)
            .MinimumLength(6).WithMessage(ErrorCodes.NotValid);
    }
}
