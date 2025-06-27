using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
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
    private readonly IConfiguration _configuration;

    public LoginEndpoint(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        var roles = user.Roles.Select(r => r.Role.Name).ToList();

        var isSuperAdmin = roles.Contains(nameof(UserRoles.SuperAdmin));

        var permissions = await GetUserPermissions(isSuperAdmin, user.Roles, cancellationToken);

        var jwtToken = JwtBearer.CreateToken(
            options: o =>
            {
                o.SigningKey = _configuration["JWTSecretKey"] ?? string.Empty;
                o.ExpireAt = DateTime.Now.AddDays(request.RememberMe ? 30 : 1);
                o.User.Roles.AddRange(roles);
                o.User.Permissions.AddRange(permissions);
                o.User.Claims.Add(("name", request.Username),
                    ("sub", user.Id.ToString()));
            });

        await SendAsync(new LoginResponse(jwtToken), cancellation: cancellationToken);
    }

    private async Task<List<string>> GetUserPermissions(bool isSuperAdmin, ICollection<UserRole> roles,
        CancellationToken cancellationToken)
    {
        if (isSuperAdmin)
        {
            return await _context.Permissions.Select(x => x.Name)
                .ToListAsync(cancellationToken: cancellationToken);
        }

        var rolesPermissions = await _context.RolePermissions
            .Where(rolePermission => roles
                .Select(userRole => userRole.RoleId)
                .Contains(rolePermission.RoleId))
            .ToListAsync(cancellationToken: cancellationToken);

        var permissions = await _context.Permissions.Where(permission =>
                rolesPermissions.Select(rolePermission => rolePermission.PermissionId).Contains(permission.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        return permissions.Select(permission => permission.Name).ToList();
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