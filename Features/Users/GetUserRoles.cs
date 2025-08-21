using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Users;

public sealed record GetUserRoleResponse(string RoleName, string RoleDescription);

public sealed class GetUserRolesEndpoint : EndpointWithoutRequest<List<GetUserRoleResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetUserRolesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/users/{id}/roles");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Users"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = Route<Guid>("id", isRequired: true);

        var user = await _context.Users
            .Include(x => x.Roles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(x => x.Id == userId,
                cancellationToken: cancellationToken);

        if (user is null)
            ThrowError(ErrorMessages.NotFound);

        await SendAsync(user.Roles.Select(x =>
                new GetUserRoleResponse(
                    x.Role.Name,
                    x.Role.Description)).ToList(),
            cancellation: cancellationToken);
    }
}