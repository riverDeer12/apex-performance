using System.Linq.Expressions;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;
    
    public AuthenticationService(ApexPerformanceContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<string> GenerateJwtToken(bool rememberMe, User user)
    {
        var roles = user.Roles.Select(r => r.Role.Name).ToList();

        var permissions = await GetUserPermissions(roles, user.Roles);

        var jwtToken = JwtBearer.CreateToken(
            options: o =>
            {
                o.SigningKey = _configuration["JWTSecretKey"] ?? string.Empty;
                o.ExpireAt = DateTime.UtcNow.AddDays(rememberMe ? 30 : 1);
                o.User.Roles.AddRange(roles);
                o.User.Permissions.AddRange(permissions);
                o.User.Claims.Add(("name", user.UserName),
                    ("sub", user.Id.ToString()));
            });
        return jwtToken;
    }
    
    private async Task<List<string>> GetUserPermissions(List<string> roles, ICollection<UserRole> userRoles)
    {
        var isSuperAdmin = roles.Contains(UserRoles.SuperAdmin);

        var admin = roles.Contains(UserRoles.Administrator);

        if (isSuperAdmin)
        {
            return await _context.Permissions.Select(x => x.Name)
                .ToListAsync();
        }

        if (admin)
        {
            Expression<Func<Permission, bool>> excludeCertainCategories = x =>
                x.Category != "Users" &&
                x.Category != "UserRoles" &&
                x.Category != "Administrators";
            
            return await _context.Permissions
                .Where(excludeCertainCategories)
                .Select(x => x.Name)
                .ToListAsync();
        }

        var rolesPermissions = await _context.RolePermissions
            .Where(rolePermission => userRoles
                .Select(userRole => userRole.RoleId)
                .Contains(rolePermission.RoleId))
            .ToListAsync();

        var permissions = await _context.Permissions.Where(permission =>
                rolesPermissions.Select(rolePermission => rolePermission.PermissionId).Contains(permission.Id))
            .ToListAsync();

        return permissions.Select(permission => permission.Name).ToList();
    }
}