using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Permissions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Roles;

public sealed record GetRoleResponse(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    List<GetPermissionResponse> Permissions,
    List<RoleUserDto> Users);

public sealed record RoleUserDto(
    Guid Id,
    string Username);

public sealed class GetRolesEndpoint : EndpointWithoutRequest<List<GetRoleResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetRolesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/roles");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Roles"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var roles = await _context
            .Roles
            .Include(role => role.Users)
            .ThenInclude(userRole => userRole.User)
            .Include(role => role.Permissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .ToListAsync(cancellationToken: cancellationToken);

        if (roles.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var response = new List<GetRoleResponse>();

        foreach (var role in roles)
        {
            var rolePermissions = role.Permissions.Select(x => x.Permission).ToList();

            var roleUsers = role.Users.Select(x => x.User).ToList();

            var permissionsResponse = rolePermissions
                .Select(x => new GetPermissionResponse(x.Id, x.Name, x.Description, x.Category))
                .ToList();

            var usersResponse = roleUsers
                .Select(x => new RoleUserDto(x.Id, x.UserName))
                .ToList();

            var roleResponse = new GetRoleResponse(role.Id, role.Name, role.Description, role.CreatedAt, role.UpdatedAt,
                role.IsDeleted, permissionsResponse, usersResponse);

            response.Add(roleResponse);
        }

        await SendAsync(response, cancellation: cancellationToken);
    }
}