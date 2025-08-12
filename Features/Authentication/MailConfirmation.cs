using System.IdentityModel.Tokens.Jwt;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Authentication;

public record MailConfirmationResponse(Guid Id, bool IsSuccess);

public class MailConfirmationEndpoint : EndpointWithoutRequest<MailConfirmationResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MailConfirmationEndpoint(ApexPerformanceContext context, IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/authentication/mail-confirmation");
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (user is null)
        {
            await SendAsync(new MailConfirmationResponse(Guid.Empty, false),
                cancellation: cancellationToken);
            return;
        }

        user.EmailConfirmed = true;

        _context.Users.Update(user);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new MailConfirmationResponse(user.Id, false),
            cancellation: cancellationToken);
    }
}