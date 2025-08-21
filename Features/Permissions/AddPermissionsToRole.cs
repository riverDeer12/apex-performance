using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Permissions;

public sealed record AddPermissionsToRoleRequest(List<Guid> Permissions);

public sealed record AddPermissionsToRoleResponse(Guid PermissionId);

public sealed class AddPermissionsToRoleEndpoint : Endpoint<AddPermissionsToRoleRequest, List<AddPermissionsToRoleResponse>>
{
    private readonly ApexPerformanceContext _context;

    public AddPermissionsToRoleEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/permissions/{roleId}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Permissions"));
    }

    public override async Task HandleAsync(AddPermissionsToRoleRequest request, CancellationToken cancellationToken)
    {
        var roleId = Route<Guid>("roleId", isRequired: true);

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == roleId,
            cancellationToken: cancellationToken);

        if (role is null)
            ThrowError(ErrorMessages.NotFound);

        var permissions = _context.Permissions
            .Where(x => request.Permissions.Contains(x.Id))
            .ToList();

        if (request.Permissions.Count != permissions.Count)
            ThrowError(ErrorMessages.NotFound);

        var newRolePermissions =
            permissions
                .Select(newRolePermission => new RolePermission { RoleId = roleId, PermissionId = newRolePermission.Id })
                .ToList();

        await _context.BulkInsertOrUpdateAsync(newRolePermissions, cancellationToken: cancellationToken);

        await SendAsync(newRolePermissions
            .Select(x => new AddPermissionsToRoleResponse(x.PermissionId))
            .ToList(), cancellation: cancellationToken);
    }
}

public sealed class AddPermissionsToRoleValidator : Validator<AddPermissionsToRoleRequest>
{
    public AddPermissionsToRoleValidator()
    {
        RuleFor(x => x.Permissions).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}