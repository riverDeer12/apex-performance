using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace ApexPerformance.API.Features.Users;

public record ResetUserPasswordRequest(
    string NewPassword
);

public record ResetUserPasswordResponse(
    Guid UserId
);

public class ResetUserPasswordEndpoint : Endpoint<ResetUserPasswordRequest, ResetUserPasswordResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;

    public ResetUserPasswordEndpoint(ICurrentUserService currentUserService, ApexPerformanceContext context,
        IConfiguration configuration)
    {
        _currentUserService = currentUserService;
        _context = context;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/users/reset-password");
        Options(x => x.WithTags("Users"));
    }

    public override async Task HandleAsync(ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            ThrowError(ErrorMessages.NotFound);

        user.Password = Database.Entities.User.HashPassword(request.NewPassword);

        _context.Users.Update(user);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        SendResetPasswordEmail(user);

        await SendAsync(new(user.Id), cancellation: cancellationToken);
    }

    private void SendResetPasswordEmail(User user)
    {
        
    }
}

public sealed class ResetPasswordValidator : Validator<ResetUserPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}