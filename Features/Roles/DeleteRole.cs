using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Roles;

public sealed record DeleteRoleResponse(Guid Id, string RoleName);

public class DeleteRoleEndpoint : EndpointWithoutRequest<DeleteRoleResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteRoleEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/roles/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Roles"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var roleId = Route<Guid>("id", isRequired: true);

        var role =
            await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken: cancellationToken);

        if (role is null)
            ThrowError(ErrorMessages.NotFound);

        role.Delete();

        _context.Roles.Update(role);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteRoleResponse(role.Id, role.Name), cancellation: cancellationToken);
    }
}