using System.IdentityModel.Tokens.Jwt;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Authentication;

public record MailConfirmationRequest(string Token);
public record MailConfirmationResponse(Guid Id, bool IsSuccess);

public class MailConfirmationEndpoint : Endpoint<MailConfirmationRequest, MailConfirmationResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;
    public MailConfirmationEndpoint(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Get("api/authentication/mail-confirmation");
        AllowAnonymous();
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(MailConfirmationRequest request, CancellationToken cancellationToken)
    {
        var handler = new JwtSecurityTokenHandler();

        var jwtToken = handler.ReadJwtToken(request.Token);

        var subValue = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (subValue is null)
            ThrowError(ValidationMessages.NotValid);

        var userId = new Guid(subValue);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId,
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

        await SendRedirectAsync(_configuration["WebAppUrl"] + "/mail-confirmation?token=" + request.Token);
    }
}