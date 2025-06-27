using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Roles;

public sealed record CreateRoleRequest(
    string Name,
    string Description,
    List<Guid> Permissions,
    List<Guid> Users);

public sealed record CreateRoleResponse(
    Guid Id,
    string Name,
    string Description);

public sealed class CreateRoleEndpoint : Endpoint<CreateRoleRequest, CreateRoleResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateRoleEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/roles");
        Roles(nameof(UserRoles.SuperAdmin));
        Options(x => x.WithTags("Roles"));
    }

    public override async Task HandleAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var newRole = Role.Init(request.Name, request.Description);

        _context.Roles.Add(newRole);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var rolePermissions = request.Permissions.Select(permissionId => new RolePermission
        {
            RoleId = newRole.Id,
            PermissionId = permissionId
        }).ToList();
        
        if (request.Users.Count is not 0)
        {
            var roleUsers = request.Users.Select(userId => new UserRole
            {
                RoleId = newRole.Id,
                UserId = userId
            }).ToList();
            
            _context.UserRoles.AddRange(roleUsers);
        }

        _context.RolePermissions.AddRange(rolePermissions);

        var relationsResult = await _context.SaveChangesAsync(cancellationToken);

        if (relationsResult == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new CreateRoleResponse(newRole.Id, newRole.Name, newRole.Description),
            cancellation: cancellationToken);
    }
}

public sealed class CreateRoleValidator : Validator<CreateRoleRequest>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Permissions).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}