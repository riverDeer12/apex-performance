using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using ApexPerformance.API.Constants;
using FastEndpoints.Security;
using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;

namespace ApexPerformance.API.Extensions;

/// <summary>
/// Dashboard is opened as a normal page, so browser does not send
/// the login token. Super admin gets a dashboard token from the API
/// and opens /jobs?token=..., the token is then kept in a cookie
/// for the dashboard's own requests.
/// The token is signed with a key derived from the JWT key, so it
/// can not be used to call the API.
/// </summary>
public class HangfireDashboardAuthPolicy : IDashboardAuthorizationFilter
{
    private const string TokenQueryParameter = "token";
    private const string CookieName = "jobs_dashboard";
    private const string PurposeClaim = "purpose";
    private const string Purpose = "jobs-dashboard";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    private readonly IConfiguration _configuration;

    public HangfireDashboardAuthPolicy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static string CreateToken(IConfiguration configuration)
        => JwtBearer.CreateToken(o =>
        {
            o.SigningKey = GetSigningKey(configuration);
            o.ExpireAt = DateTime.UtcNow.Add(TokenLifetime);
            o.User.Claims.Add((PurposeClaim, Purpose));
        });

    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();

        if (http.User?.FindFirst("role")?.Value == UserRoles.SuperAdmin)
            return true;

        var queryToken = http.Request.Query[TokenQueryParameter].ToString();

        if (!string.IsNullOrEmpty(queryToken) && IsValid(queryToken))
        {
            http.Response.Cookies.Append(CookieName, queryToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = TokenLifetime
            });

            return true;
        }

        return http.Request.Cookies.TryGetValue(CookieName, out var cookieToken) && IsValid(cookieToken);
    }

    private bool IsValid(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSigningKey(_configuration))),
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            return principal.FindFirst(PurposeClaim)?.Value == Purpose;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetSigningKey(IConfiguration configuration)
    {
        var jwtKey = configuration["JWTSecretKey"] ?? string.Empty;

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(jwtKey + ":" + Purpose)));
    }
}
