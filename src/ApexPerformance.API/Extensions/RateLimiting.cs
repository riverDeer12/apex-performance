using System.Threading.RateLimiting;
using ApexPerformance.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ApexPerformance.API.Extensions;

/// <summary>
/// Limits for anonymous endpoints that scripts could abuse: registration
/// and forgotten password send emails and create data, login can be used
/// to guess passwords. Limits are per client IP address, registration
/// also has a limit for all clients together.
/// </summary>
public static class RateLimiting
{
    public const string Registration = "registration";
    public const string Login = "login";
    public const string PasswordReset = "password-reset";
    public const string ChatMessages = "chat-messages";

    private const string RegistrationPath = "/api/users/register";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Registration, context =>
                FixedWindowPerIp(context, permits: 5, window: TimeSpan.FromHours(1)));

            options.AddPolicy(Login, context =>
                FixedWindowPerIp(context, permits: 30, window: TimeSpan.FromMinutes(5)));

            options.AddPolicy(PasswordReset, context =>
                FixedWindowPerIp(context, permits: 5, window: TimeSpan.FromMinutes(15)));

            // Protects against flooding a chat with a script, normal typing stays far below it.
            options.AddPolicy(ChatMessages, context =>
                FixedWindowPerIp(context, permits: 120, window: TimeSpan.FromMinutes(1)));

            // Many addresses (a botnet) can't flood registration either.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments(RegistrationPath)
                    ? RateLimitPartition.GetFixedWindowLimiter("registration-all", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    })
                    : RateLimitPartition.GetNoLimiter("none"));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                // Same shape as other API errors, so the web application shows the message.
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    statusCode = StatusCodes.Status429TooManyRequests,
                    message = "Too many requests.",
                    errors = new { generalErrors = new[] { ErrorCodes.TooManyRequests } }
                }, cancellationToken);
            };
        });

    // IIS passes the address of the connecting client, which a script can't fake
    // (unlike forwarded headers, which are not trusted here).
    private static RateLimitPartition<string> FixedWindowPerIp(HttpContext context, int permits, TimeSpan window)
        => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0
            });
}
