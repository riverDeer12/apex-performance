using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Users;

public sealed record CreateUserRequest(string Username, string Password, string Email, List<Guid> Roles);

public sealed record CreateUserResponse(Guid Id, string Username);

public sealed class CreateUserEndpoint : Endpoint<CreateUserRequest, CreateUserResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IUserService _userService;

    public CreateUserEndpoint(ApexPerformanceContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public override void Configure()
    {
        Post("api/users");
        Permissions(nameof(UserPermissions.CanCreateUser));
        Options(x => x.WithTags("Users"));
    }

    public override async Task HandleAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var userAccount =
            await _userService.CreateUserAccount(request.Username, request.Password, request.Email, cancellationToken);

        var userRoles = request.Roles.Select(roleId => new UserRole
        {
            RoleId = roleId,
            UserId = userAccount.Id
        }).ToList();

        _context.UserRoles.AddRange(userRoles);

        var userRolesResult = await _context.SaveChangesAsync(cancellationToken);

        if (userRolesResult == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new CreateUserResponse(userAccount.Id, userAccount.UserName), cancellation: cancellationToken);
    }
}

public sealed class CreateUserValidator : Validator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Password).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Roles).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}