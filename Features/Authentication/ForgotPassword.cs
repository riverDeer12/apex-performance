using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FastEndpoints.Security;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace ApexPerformance.API.Features.Authentication;

public record ForgotPasswordRequest(
    string Email
);

public record ForgotPasswordResponse(
);

public class ForgotPasswordEndpoint : Endpoint<ForgotPasswordRequest, ForgotPasswordResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public ForgotPasswordEndpoint(ApexPerformanceContext context, IConfiguration configuration,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Post("api/authentication/forgot-password");
        AllowAnonymous();
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user == null)
        {
            await SendAsync(new ForgotPasswordResponse(), cancellation: cancellationToken);
            return;
        }
        
        var token = JwtBearer.CreateToken(
            options: o =>
            {
                o.SigningKey = _configuration["JWTSecretKey"] ?? string.Empty;
                o.ExpireAt = DateTime.Now.AddMinutes(10);
                o.User.Claims.Add(("name", user.UserName),
                    ("sub", user.Id.ToString()));
            });

        _emailService.SendForgotPasswordEmail(user, token);

        await SendAsync(new ForgotPasswordResponse(), cancellation: cancellationToken);
    }
}