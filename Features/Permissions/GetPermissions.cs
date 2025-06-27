using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Permissions;

public sealed record GetPermissionResponse(Guid Id, string Name, string Description, string Category);

public sealed class GetPermissionsEndpoint : EndpointWithoutRequest<List<GetPermissionResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetPermissionsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/permissions");
        Roles(nameof(UserRoles.SuperAdmin));
        Options(x => x.WithTags("Permissions"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions.ToListAsync(cancellationToken: cancellationToken);

        await SendAsync(permissions
            .Select(x => new GetPermissionResponse(x.Id, x.Name, x.Description, x.Category))
            .ToList(), cancellation: cancellationToken);
    }
}