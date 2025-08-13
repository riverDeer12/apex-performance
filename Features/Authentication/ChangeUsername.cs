using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Authentication;

public record ChangeUsernameRequest(string Username);

public record ChangeUsernameResponse(Guid Id);

public class ChangeUsernameEndpoint : Endpoint<ChangeUsernameRequest, ChangeUsernameResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public ChangeUsernameEndpoint(IUserService userService, ApexPerformanceContext context,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/authentication/change-username");
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(ChangeUsernameRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (user is null)
            ThrowError(ErrorMessages.NotFound);

        if (await _userService.UsernameExists(request.Username, cancellationToken))
            ThrowError(ValidationMessages.UsernameAlreadyExists);

        user.UserName = request.Username;

        _context.Users.Update(user);
        
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new ChangeUsernameResponse(user.Id), cancellation: cancellationToken);
    }
}

public sealed class ChangeUsernameValidator : Validator<ChangeUsernameRequest>
{
    public ChangeUsernameValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}