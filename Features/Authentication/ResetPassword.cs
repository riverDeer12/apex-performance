using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FastEndpoints.Security;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace ApexPerformance.API.Features.Authentication;

public record ResetPasswordRequest(
    string Email
);

public record ResetPasswordResponse(
);

public class ResetPasswordEndpoint : Endpoint<ResetPasswordRequest, ResetPasswordResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;

    public ResetPasswordEndpoint(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/authentication/reset-password");
        Options(x => x.WithTags("Authentication"));
    }

    public override async Task HandleAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user == null)
        {
            await SendAsync(new ResetPasswordResponse(), cancellation: cancellationToken);
            return;
        }

        SendResetPasswordEmail(user);
        
        await SendAsync(new ResetPasswordResponse(), cancellation: cancellationToken);
    }

    private void SendResetPasswordEmail(User user)
    {
        var message = new MimeMessage();
        
        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));
        
        message.To.Add(new MailboxAddress(user.UserName, user.Email));
        
        message.Subject = "Reset Password Link";
        
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ResetPasswordEmail.html");
        
        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", user.UserName);
        
        html = html.Replace("{{ResetPasswordLink}}", CreateResetPasswordLink(user));

        message.Body = new TextPart("html") { Text = html };

        using var smtpClient = new SmtpClient();

        try
        {
            smtpClient.Connect(_configuration["MailConfiguration::Host"],
                int.Parse(_configuration["MailConfiguration::Port"]!),
                MailKit.Security.SecureSocketOptions.StartTls);
            smtpClient.Authenticate(_configuration["MailConfiguration::Username"],
                _configuration["MailConfiguration::Password"]);
            smtpClient.Send(message);
            smtpClient.Disconnect(true);
            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }

    private string CreateResetPasswordLink(User user)
    {
        var token = JwtBearer.CreateToken(
            options: o =>
            {
                o.SigningKey = _configuration["JWTSecretKey"] ?? string.Empty;
                o.ExpireAt = DateTime.Now.AddMinutes(10);
                o.User.Claims.Add(("name", user.UserName),
                    ("sub", user.Id.ToString()));
            });
            
            
        return  $"{_configuration["WebAppUrl"]}/authentication/reset-password?token={token}";
    }
}