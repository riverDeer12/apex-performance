using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Permissions;

public sealed record RemovePermissionsFromRoleRequest(List<Guid> PermissionIds);

public sealed record RemovePoliciesFromRoleResponse(Guid PermissionId);

public sealed class
    RemovePoliciesFromRoleEndpoint : Endpoint<RemovePermissionsFromRoleRequest, List<RemovePoliciesFromRoleResponse>>
{
    private readonly ApexPerformanceContext _context;

    public RemovePoliciesFromRoleEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/permissions/{roleId}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Permissions"));
    }

    public override async Task HandleAsync(RemovePermissionsFromRoleRequest request, CancellationToken cancellationToken)
    {
        var roleId = Route<Guid>("roleId", isRequired: true);

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == roleId,
            cancellationToken: cancellationToken);

        if (role is null)
            ThrowError(ErrorMessages.NotFound);

        var permissions = _context.Permissions
            .Where(x => request.PermissionIds.Contains(x.Id))
            .ToList();

        if (request.PermissionIds.Count != permissions.Count)
            ThrowError(ErrorMessages.NotFound);

        var rolePolicies = _context.RolePermissions
            .Where(x => x.RoleId == roleId && request.PermissionIds.Contains(x.PermissionId)).ToList();

        _context.RolePermissions.RemoveRange(rolePolicies);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result != rolePolicies.Count)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(rolePolicies
            .Select(x => new RemovePoliciesFromRoleResponse(x.PermissionId))
            .ToList(), cancellation: cancellationToken);
    }
}

public sealed class RemovePermissionFromRoleValidator : Validator<RemovePermissionsFromRoleRequest>
{
    public RemovePermissionFromRoleValidator()
    {
        RuleFor(x => x.PermissionIds).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}